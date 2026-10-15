import {spawn} from 'node:child_process';import path from 'node:path';
const installed=path.join(process.env.LOCALAPPDATA,'Programs','MehrDownload','NativeHost.exe'),mode=process.argv[2]||'ping';
const message=mode==='ping'?{action:'ping'}:{url:'http://127.0.0.1:18743/confirmation-test.bin'};
const proc=spawn(installed,[],{stdio:['pipe','pipe','pipe']});const text=Buffer.from(JSON.stringify(message)),header=Buffer.alloc(4);header.writeUInt32LE(text.length);let output=Buffer.alloc(0);proc.stdout.on('data',bytes=>{output=Buffer.concat([output,bytes]);if(output.length>=4&&output.length>=4+output.readUInt32LE()){const reply=JSON.parse(output.subarray(4,4+output.readUInt32LE()).toString());console.log(JSON.stringify(reply));process.exit((mode==='ping'&&!reply.accepted)||(mode==='cancel'&&!reply.cancelled)?1:0);}});proc.stderr.on('data',()=>{});proc.stdin.end(Buffer.concat([header,text]));
setTimeout(()=>{proc.kill();console.error('Native response timed out');process.exit(1);},140000);
