// SPDX-License-Identifier: GPL-3.0-or-later
package main
import "testing"
func TestURLValidation(t *testing.T){for _,s:=range []string{"https://example.com/file.zip","http://127.0.0.1/test"}{if !validURL(s){t.Fatal(s)}};for _,s:=range []string{"file:///C:/secret","javascript:alert(1)","https://user:pass@example.com/file","https:///file"}{if validURL(s){t.Fatal(s)}}}
