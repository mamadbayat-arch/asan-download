import 'dart:io';
import 'dart:ui' as ui;
import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter/services.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:get/get.dart';
import 'package:gopeed/asan/sidebar.dart';
import 'package:gopeed/asan/technical_text.dart';
import 'package:gopeed/asan/task_statistics.dart';
import 'package:gopeed/app/modules/extension/controllers/extension_controller.dart';
import 'package:gopeed/app/modules/extension/views/extension_view.dart';
import 'package:gopeed/api/model/store_extension.dart';
import 'package:gopeed/i18n/message.dart';
import 'package:gopeed/theme/theme.dart';

class OfflineExtensionController extends ExtensionController {
  @override Future<void> onInit() async {}
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  setUpAll(() async {
    final loader = FontLoader('Kalameh')
      ..addFont(rootBundle.load('assets/fonts/Kalameh-Regular.ttf'))
      ..addFont(rootBundle.load('assets/fonts/Kalameh-Bold.ttf'));
    await loader.load();
  });
  tearDown(() { Get.reset(); });

  test('Persian covers every app label and preserves interpolation variables', () {
    final en = messages.keys['en_US']!, fa = messages.keys['fa_IR']!;
    for (final entry in en.entries) {
      expect(fa.containsKey(entry.key), isTrue, reason: entry.key);
      expect(RegExp(r'@\w+').allMatches(fa[entry.key]!).map((m) => m[0]).toSet(),
        RegExp(r'@\w+').allMatches(entry.value).map((m) => m[0]).toSet(), reason: entry.key);
    }
  });

  for (final brightness in Brightness.values) {
    for (final width in [390.0, 800.0, 1200.0]) {
      testWidgets('Persian extension page fits $width in ${brightness.name}', (tester) async {
        tester.view.physicalSize = Size(width, 820);
        tester.view.devicePixelRatio = 1;
        addTearDown(tester.view.resetPhysicalSize);
        addTearDown(tester.view.resetDevicePixelRatio);
        Get.testMode = true;
        final controller = Get.put<ExtensionController>(OfflineExtensionController());
        controller.storeExtensions.add(StoreExtension(
          id:'example',repoFullName:'example/extension',repoUrl:'https://example.com/source',
          name:'example',author:'Example Author',title:'Example download extension',
          description:'توضیح نمونهٔ افزونه برای بررسی خوانایی متن فارسی و فاصلهٔ درست سطرها.',
          version:'1.2.3',installCount:1234,stars:567,topics:const [],
        ));
        final boundary = GlobalKey();
        await tester.pumpWidget(GetMaterialApp.router(
          locale: const Locale('fa','IR'), fallbackLocale: const Locale('en','US'),
          translations: messages, theme: GopeedTheme.light, darkTheme: GopeedTheme.dark,
          themeMode: brightness == Brightness.dark ? ThemeMode.dark : ThemeMode.light,
          supportedLocales: const [Locale('fa','IR'),Locale('en','US')],
          localizationsDelegates: GlobalMaterialLocalizations.delegates,
          builder:(context,child)=>MediaQuery(data:MediaQuery.of(context).copyWith(
            textScaler:TextScaler.linear(width==390 ? 1.3 : 1)),child:child!),
          getPages: [GetPage(name:'/', page:()=>RepaintBoundary(key:boundary,child:Scaffold(body:Row(children:[
            if(width>=768) AsanSidebar(selectedIndex:1,onSelect:(_){},onHistory:(){},onReport:(){},footer:const SizedBox.shrink()),
            Expanded(child:ExtensionView()),
          ]))))],
          initialRoute:'/',
        ));
        await tester.pumpAndSettle();
        expect(tester.takeException(), isNull);
        expect(find.text('همهٔ افزونه‌ها'), findsOneWidget);
        expect(find.text('نصب با لینک'), findsOneWidget);
        expect(find.text('搜索扩展...'), findsNothing);
        final text = tester.widget<Text>(find.text('همهٔ افزونه‌ها'));
        final element = tester.element(find.text('همهٔ افزونه‌ها'));
        expect(Directionality.of(element), TextDirection.rtl);
        expect(Theme.of(element).textTheme.bodyMedium!.fontFamily,'Kalameh');
        expect(Theme.of(element).textTheme.bodyMedium!.height,greaterThanOrEqualTo(1.5));
        expect(text.textAlign ?? TextAlign.start, TextAlign.start);
        if(width>=768) {
          final labels = ['دانلودها','افزونه‌ها','تنظیمات'];
          final edges = labels.map((label)=>tester.getRect(find.text(label)).right).toList();
          expect(edges.every((value)=>(value-edges.first).abs()<1),isTrue);
        }
        if(Platform.environment['ASAN_UI_SHOTS']=='1') {
          final render=boundary.currentContext!.findRenderObject()! as RenderRepaintBoundary;
          final image=await render.toImage(pixelRatio:1);
          final bytes=await image.toByteData(format:ui.ImageByteFormat.png);
          final output=File('build/asan-ui/extensions-${brightness.name}-${width.toInt()}.png');
          await output.parent.create(recursive:true);
          await output.writeAsBytes(bytes!.buffer.asUint8List()); image.dispose();
        }
      });
    }
  }
  testWidgets('mixed filenames and metrics retain order in a narrow RTL layout', (tester) async {
    await tester.pumpWidget(MaterialApp(theme:GopeedTheme.light,home:Directionality(
      textDirection:TextDirection.rtl,child:Scaffold(body:SizedBox(width:290,child:Column(
        crossAxisAlignment:CrossAxisAlignment.stretch,children:const [
          AsanTechnicalText('Cherry.Studio.2.1.6.x64.rar'),
          AsanDownloadMetrics(date:'1405/07/24 12:30',duration:'00:02:14',average:'12.34 MB/s'),
        ]))))));
    await tester.pumpAndSettle();
    expect(tester.takeException(),isNull);
    final name=tester.widget<Text>(find.text('Cherry.Studio.2.1.6.x64.rar'));
    expect(name.textDirection,TextDirection.ltr);expect(name.textAlign,TextAlign.right);
    expect(tester.widget<Text>(find.text('12.34 MB/s')).textDirection,TextDirection.ltr);
  });
}
