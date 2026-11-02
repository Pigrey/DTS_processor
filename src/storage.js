let revision=0;
let queue=Promise.resolve();
let loaded=false;
export let storagePath='';
export async function loadData(){
 const res=await fetch('/api/data',{cache:'no-store'});
 if(!res.ok)throw new Error('无法读取本地数据文件，请通过事项簿程序启动应用。');
 const result=await res.json();
 if(!Number.isInteger(result.revision)||typeof result.path!=='string')throw new Error('本地数据服务响应异常。');
 revision=result.revision;storagePath=result.path;loaded=true;return result.data;
}
export function persistData(data){
 const snapshot=JSON.stringify(data);
 const op=queue.catch(()=>{}).then(async()=>{
  if(!loaded)throw new Error('本地数据尚未加载，已停止保存以保护原文件。');
  const res=await fetch('/api/data',{method:'PUT',headers:{'Content-Type':'application/json','X-Task-Request':'1'},body:`{"revision":${revision},"data":${snapshot}}`});
  if(res.status===409){loaded=false;throw new Error('其他窗口已修改数据，请先复制未保存内容，再刷新页面。');}
  if(!res.ok)throw new Error('本地文件保存失败，请检查磁盘空间及文件权限，并导出备份。');
  const result=await res.json();revision=result.revision;
 });
 queue=op;return op;
}
