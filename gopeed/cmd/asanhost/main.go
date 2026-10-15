// SPDX-License-Identifier: GPL-3.0-or-later
// Browser bridge for the Asan Download fork. Gopeed upstream remains credited.
package main

import (
 "bytes"
 "context"
 "encoding/binary"
 "encoding/json"
 "fmt"
 "io"
 "net"
 "net/http"
 "net/url"
 "os"
 "os/exec"
 "path/filepath"
 "strings"
 "time"
 "github.com/Microsoft/go-winio"
)
type request struct { URL string `json:"url"`; Action string `json:"action"` }
type response struct { Accepted bool `json:"accepted"`; Cancelled bool `json:"cancelled"`; Reason string `json:"reason"` }
func validURL(value string) bool { u,err:=url.Parse(value);return err==nil&&len(value)<=8192&&u.Host!=""&&u.User==nil&&(u.Scheme=="https"||u.Scheme=="http") }
func dial() (net.Conn,error) { timeout:=1500*time.Millisecond;return winio.DialPipe(`\\.\pipe\asan_download_host`,&timeout) }
func ensureApp() error {
 if c,err:=dial();err==nil {c.Close();return nil}
 self,err:=os.Executable();if err!=nil{return err};exe:=filepath.Join(filepath.Dir(self),"AsanDownload.exe")
 if data,err:=os.ReadFile(filepath.Join(filepath.Dir(self),"asan-app-path.txt"));err==nil{exe=strings.TrimSpace(string(data))}
 if filepath.Ext(exe)!=".exe"{return fmt.Errorf("invalid app path")};if _,err=os.Stat(exe);err!=nil{return err}
 cmd:=exec.Command(exe,"--hidden");cmd.Dir=filepath.Dir(exe);if err=cmd.Start();err!=nil{return err};go cmd.Wait()
 for n:=0;n<30;n++{time.Sleep(500*time.Millisecond);if c,e:=dial();e==nil{c.Close();return nil}}
 return fmt.Errorf("app unavailable")
}
func forward(req request) response {
 if req.Action!="ping"&&req.Action!="open"&&!validURL(req.URL){return response{Reason:"invalid_url"}}
 if err:=ensureApp();err!=nil{return response{Reason:"unavailable"}}
 endpoint:="/asan-create";if req.Action=="ping"{endpoint="/asan-ping"};if req.Action=="open"{endpoint="/asan-activate"}
 body,_:=json.Marshal(req);client:=&http.Client{Transport:&http.Transport{DialContext:func(context.Context,string,string)(net.Conn,error){return dial()}},Timeout:120*time.Second}
 result,err:=client.Post("http://127.0.0.1"+endpoint,"application/json",bytes.NewReader(body));if err!=nil{return response{Reason:"unavailable"}};defer result.Body.Close()
 var reply response;if err=json.NewDecoder(io.LimitReader(result.Body,16384)).Decode(&reply);err!=nil{return response{Reason:"invalid_response"}};return reply
}
func main(){reply:=response{Reason:"invalid_message"};var size uint32
 if err:=binary.Read(os.Stdin,binary.LittleEndian,&size);err==nil&&size>0&&size<=16384{body:=make([]byte,size);if _,err=io.ReadFull(os.Stdin,body);err==nil{var message request;if json.Unmarshal(body,&message)==nil{reply=forward(message)}}}
 body,_:=json.Marshal(reply);binary.Write(os.Stdout,binary.LittleEndian,uint32(len(body)));os.Stdout.Write(body)
}
