import {defineConfig} from 'vite';
export default defineConfig({server:{proxy:{'/api':{target:'http://127.0.0.1:17643',changeOrigin:true,configure(proxy){proxy.on('proxyReq',(proxyReq,req)=>{if(['http://localhost:5173','http://127.0.0.1:5173'].includes(req.headers.origin))proxyReq.setHeader('Origin','http://127.0.0.1:17643');});}}}}});
