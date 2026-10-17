// SPDX-License-Identifier: GPL-3.0-or-later
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

class AsanLicensesPage extends StatelessWidget {
  const AsanLicensesPage({super.key});
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('مجوزها و سازندگان')),
    body: ListView(padding: const EdgeInsets.all(24), children: [
      Text('آسان دانلود', style: Theme.of(context).textTheme.headlineSmall),
      const SizedBox(height: 16),
      const SelectableText('این نرم‌افزار نسخهٔ اصلاح‌شدهٔ Gopeed 1.9.3، ساختهٔ GopeedLab و مشارکت‌کنندگان آن، تحت مجوز GNU GPL نسخهٔ ۳ است. نام و نشان آسان دانلود متعلق به این نسخه است و به معنی تأیید آن از سوی سازندگان پروژهٔ اصلی نیست.\n\nتغییرات رابط فارسی، اتصال مرورگر و بسته‌بندی ویندوز توسط مهر نور انجام شده است. مجوز و اطلاعیهٔ حقوق سازندگان در فایل‌های LICENSE و ASAN-NOTICE.md همراه نرم‌افزار قرار دارند.'),
      const SizedBox(height: 20),
      ListTile(leading: const Icon(Icons.code_rounded), title: const Text('سورس متناظر همین نسخه و مجوز کامل'),
        onTap: () => launchUrl(Uri.parse('https://downloader.hamtavar.ir/licenses.html'), mode: LaunchMode.externalApplication)),
      ListTile(leading: const Icon(Icons.open_in_new_rounded), title: const Text('سازندگان موتور متن‌باز'),
        onTap: () => launchUrl(Uri.parse('https://github.com/GopeedLab/gopeed/tree/v1.9.3'), mode: LaunchMode.externalApplication)),
      ListTile(leading: const Icon(Icons.description_outlined), title: const Text('مجوز کتابخانه‌های استفاده‌شده'),
        onTap: () => showLicensePage(context: context, applicationName: 'آسان دانلود')),
    ]),
  );
}
