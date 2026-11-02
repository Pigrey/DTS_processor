package main

import (
	"bytes"
	"encoding/json"
	"net/http/httptest"
	"os"
	"path/filepath"
	"testing"
)

func request(s *store, method, body, origin string) *httptest.ResponseRecorder {
	r := httptest.NewRequest(method, appURL+"/api/data", bytes.NewBufferString(body))
	r.Header.Set("X-Task-Request", "1")
	if origin != "" {
		r.Header.Set("Origin", origin)
	}
	w := httptest.NewRecorder()
	s.handler(w, r)
	return w
}
func TestFilePersistenceAndConflicts(t *testing.T) {
	file := filepath.Join(t.TempDir(), "tasks.json")
	s, err := openStore(file)
	if err != nil {
		t.Fatal(err)
	}
	body := `{"revision":0,"data":{"tasks":[{"title":"验证事项"}],"columns":[],"hidden":[],"mode":"selected"}}`
	if w := request(s, "PUT", body, appURL); w.Code != 200 {
		t.Fatal(w.Code, w.Body.String())
	}
	restored, err := openStore(file)
	if err != nil {
		t.Fatal(err)
	}
	if restored.doc.Revision != 1 {
		t.Fatal("revision not restored")
	}
	if w := request(restored, "GET", "", ""); !bytes.Contains(w.Body.Bytes(), []byte("验证事项")) {
		t.Fatal("missing saved data")
	}
	if w := request(s, "PUT", body, appURL); w.Code != 409 {
		t.Fatal("stale write not rejected")
	}
	if w := request(s, "PUT", body, "https://evil.example"); w.Code != 403 {
		t.Fatal("cross-origin write not rejected")
	}
	if w := request(s, "PUT", `{"revision":1,"data":{"tasks":[],"columns":[]}}`, appURL); w.Code != 200 {
		t.Fatal(w.Body.String())
	}
	backup, err := os.ReadFile(file + ".bak")
	if err != nil {
		t.Fatal(err)
	}
	var d document
	if json.Unmarshal(backup, &d) != nil || d.Revision != 1 {
		t.Fatal("incorrect backup")
	}
}
func TestCorruptFilePreserved(t *testing.T) {
	file := filepath.Join(t.TempDir(), "tasks.json")
	os.WriteFile(file, []byte("broken"), 0600)
	if _, err := openStore(file); err == nil {
		t.Fatal("corrupt data accepted")
	}
	b, _ := os.ReadFile(file)
	if string(b) != "broken" {
		t.Fatal("corrupt original modified")
	}
}
