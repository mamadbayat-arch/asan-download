import 'package:flutter_test/flutter_test.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart';
import 'package:gopeed/asan/browser_confirmation.dart';
void main(){test('browser links require HTTP(S), a host and no embedded credentials',(){expect(validBrowserUrl('https://example.com/file.zip'),isTrue);expect(validBrowserUrl('file:///C:/data'),isFalse);expect(validBrowserUrl('https://user:pass@example.com/file'),isFalse);expect(validBrowserUrl('https:///file'),isFalse);});
 for(final accept in [false,true]){testWidgets('browser request stays pending until an explicit ${accept?'accept':'cancel'}',(tester)async{
  Get.testMode=true;bool? answer;
  await tester.pumpWidget(GetMaterialApp(home:Scaffold(body:TextButton(onPressed:()async{answer=await confirmBrowserDownload('https://example.com/file.zip?private=value');},child:const Text('open'))),builder:(_,child)=>Overlay(initialEntries:[OverlayEntry(builder:(_)=>child!)])));
  await tester.tap(find.text('open'));await tester.pumpAndSettle();expect(answer,isNull);expect(find.text('این فایل دانلود شود؟'),findsOneWidget);expect(find.textContaining('private=value'),findsNothing);
  await tester.tap(find.text(accept?'دانلود کن':'انصراف'));await tester.pumpAndSettle();expect(answer,accept);await Get.reset();
 });}
}
