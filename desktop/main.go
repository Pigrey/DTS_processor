package main

import (
	"bytes"
	"embed"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"log"
	"net"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
	"strings"
	"sync"
	"time"
)

//go:embed web/*
var assets embed.FS

const addr = "127.0.0.1:17643"
const appURL = "http://" + addr

type document struct {
	Revision int             `json:"revision"`
	Data     json.RawMessage `json:"data"`
}
type store struct {
	mu   sync.Mutex
	file string
	doc  document
}

func openStore(file string) (*store, error) {
	s := &store{file: file, doc: document{Data: json.RawMessage("null")}}
	b, err := os.ReadFile(file)
	if errors.Is(err, os.ErrNotExist) {
		return s, nil
	}
	if err != nil {
		return nil, err
	}
	if err = json.Unmarshal(b, &s.doc); err != nil {
		return nil, fmt.Errorf("data file is invalid; original file preserved: %w", err)
	}
	if s.doc.Revision < 0 || !validData(s.doc.Data) {
		return nil, errors.New("data file is invalid; original file preserved")
	}
	return s, nil
}
func validData(b json.RawMessage) bool {
	var d struct {
		Tasks   []json.RawMessage `json:"tasks"`
		Columns []json.RawMessage `json:"columns"`
	}
	if len(b) == 0 || bytes.Equal(b, []byte("null")) {
		return false
	}
	if json.Unmarshal(b, &d) != nil || d.Tasks == nil || d.Columns == nil {
		return false
	}
	return true
}
func atomicWrite(file string, b []byte) error {
	f, err := os.CreateTemp(filepath.Dir(file), ".tasks-*.tmp")
	if err != nil {
		return err
	}
	name := f.Name()
	defer os.Remove(name)
	if err = f.Chmod(0600); err == nil {
		_, err = f.Write(b)
	}
	if err == nil {
		err = f.Sync()
	}
	closeErr := f.Close()
	if err == nil {
		err = closeErr
	}
	if err != nil {
		return err
	}
	return os.Rename(name, file)
}
func (s *store) handler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Cache-Control", "no-store")
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	if r.Host != addr {
		http.Error(w, "Invalid host", http.StatusForbidden)
		return
	}
	if origin := r.Header.Get("Origin"); origin != "" && origin != appURL {
		http.Error(w, "Invalid origin", http.StatusForbidden)
		return
	}
	s.mu.Lock()
	defer s.mu.Unlock()
	switch r.Method {
	case "GET":
		json.NewEncoder(w).Encode(struct {
			document
			Path string `json:"path"`
		}{s.doc, s.file})
	case "PUT":
		if r.Header.Get("X-Task-Request") != "1" {
			http.Error(w, "Missing request header", http.StatusForbidden)
			return
		}
		var next document
		dec := json.NewDecoder(http.MaxBytesReader(w, r.Body, 20<<20))
		if dec.Decode(&next) != nil || !validData(next.Data) {
			http.Error(w, "Invalid data (limit 20 MB)", http.StatusBadRequest)
			return
		}
		var extra any
		if dec.Decode(&extra) != io.EOF {
			http.Error(w, "Invalid trailing data", http.StatusBadRequest)
			return
		}
		if next.Revision != s.doc.Revision {
			http.Error(w, "Data changed in another window. Reload before editing.", http.StatusConflict)
			return
		}
		next.Revision++
		b, err := json.MarshalIndent(next, "", "  ")
		if err == nil && s.doc.Revision > 0 {
			var old []byte
			old, err = os.ReadFile(s.file)
			if err == nil {
				err = atomicWrite(s.file+".bak", old)
			}
		}
		if err == nil {
			err = atomicWrite(s.file, b)
		}
		if err != nil {
			log.Print(err)
			http.Error(w, "Could not save local file", 500)
			return
		}
		s.doc = next
		json.NewEncoder(w).Encode(map[string]int{"revision": next.Revision})
	default:
		w.Header().Set("Allow", "GET, PUT")
		http.Error(w, "Method not allowed", 405)
	}
}
func dataDir() (string, error) {
	if dir := os.Getenv("TASK_TRACKER_DATA_DIR"); dir != "" {
		return filepath.Abs(dir)
	}
	home, err := os.UserHomeDir()
	if err != nil {
		return "", err
	}
	documents := filepath.Join(home, "Documents")
	if runtime.GOOS == "windows" {
		out, err := exec.Command("powershell.exe", "-NoProfile", "-NonInteractive", "-Command", "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; [Environment]::GetFolderPath('MyDocuments')").Output()
		if err != nil {
			return "", fmt.Errorf("cannot locate Windows Documents folder: %w", err)
		}
		documents = strings.TrimSpace(strings.TrimPrefix(string(out), "\ufeff"))
		if documents == "" {
			return "", errors.New("Windows Documents folder is unavailable")
		}
	}
	return filepath.Join(documents, "事项簿"), nil
}
func openBrowser() {
	if os.Getenv("TASK_TRACKER_NO_BROWSER") == "1" {
		return
	}
	var cmd *exec.Cmd
	switch runtime.GOOS {
	case "windows":
		cmd = exec.Command("rundll32.exe", "url.dll,FileProtocolHandler", appURL)
	case "darwin":
		cmd = exec.Command("open", appURL)
	default:
		cmd = exec.Command("xdg-open", appURL)
	}
	if err := cmd.Run(); err != nil {
		log.Printf("Open %s in your browser (%v)", appURL, err)
	}
}
func main() {
	dir, err := dataDir()
	if err != nil {
		log.Fatal(err)
	}
	if err = os.MkdirAll(dir, 0700); err != nil {
		log.Fatal(err)
	}
	s, err := openStore(filepath.Join(dir, "tasks.json"))
	if err != nil {
		log.Fatal(err)
	}
	listener, err := net.Listen("tcp", addr)
	if err != nil {
		client := http.Client{Timeout: 2 * time.Second}
		res, e := client.Get(appURL + "/health")
		if e == nil {
			defer res.Body.Close()
			b, _ := io.ReadAll(io.LimitReader(res.Body, 100))
			if string(b) == "shixiang-tracker-v1" {
				openBrowser()
				return
			}
		}
		log.Fatalf("Port 17643 is unavailable: %v", err)
	}
	mux := http.NewServeMux()
	mux.HandleFunc("/api/data", s.handler)
	mux.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) { fmt.Fprint(w, "shixiang-tracker-v1") })
	mux.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		if r.Host != addr {
			http.Error(w, "Invalid host", 403)
			return
		}
		if r.Method != "GET" && r.Method != "HEAD" {
			http.Error(w, "Method not allowed", 405)
			return
		}
		path := strings.TrimPrefix(r.URL.Path, "/")
		if path == "" {
			path = "index.html"
		}
		b, err := assets.ReadFile("web/" + path)
		if err != nil {
			http.NotFound(w, r)
			return
		}
		w.Header().Set("X-Content-Type-Options", "nosniff")
		http.ServeContent(w, r, path, time.Time{}, bytes.NewReader(b))
	})
	fmt.Printf("事项簿 | %s\n数据文件：%s\n关闭此窗口即可退出应用。请等待页面显示保存成功后关闭。\n", appURL, s.file)
	go openBrowser()
	server := &http.Server{Handler: mux, ReadHeaderTimeout: 5 * time.Second, ReadTimeout: 30 * time.Second, WriteTimeout: 30 * time.Second, IdleTimeout: 60 * time.Second}
	log.Fatal(server.Serve(listener))
}
