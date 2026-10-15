import crypto from 'node:crypto';import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';
const home=path.dirname(fileURLToPath(import.meta.url)), data=path.resolve(process.env.DATA_DIR||path.join(home,'data'));
const password=process.env.ADMIN_PASSWORD;if(!password||password.length<12)throw new Error('Set ADMIN_PASSWORD with at least 12 characters. It is read from environment, never command arguments.');
const salt=crypto.randomBytes(32).toString('hex'),hash=crypto.scryptSync(password,salt,64).toString('hex');fs.mkdirSync(data,{recursive:true});
fs.writeFileSync(path.join(data,'admin.json'),JSON.stringify({username:process.env.ADMIN_USERNAME||'admin',salt,hash}),{mode:0o600});console.log('Admin credentials initialized.');
