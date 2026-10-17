// SPDX-License-Identifier: GPL-3.0-or-later
import 'dart:convert';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package_info.dart';

class VersionInfo {
  final String version;
  final String changeLog;
  VersionInfo(this.version, this.changeLog);
}

// Compare complete versions numerically; a lower major must never be treated
// as newer merely because its minor or patch component is higher.
int compareAsanVersions(String left, String right) {
  List<int> parts(String value) {
    if (!RegExp(r'^\d{1,5}\.\d{1,5}\.\d{1,5}$').hasMatch(value)) {
      throw const FormatException('Invalid release version');
    }
    return value.split('.').map(int.parse).toList();
  }
  final a = parts(left), b = parts(right);
  for (var i = 0; i < 3; i++) {
    final diff = a[i].compareTo(b[i]);
    if (diff != 0) return diff;
  }
  return 0;
}

VersionInfo? parseAsanRelease(Map<String, dynamic> json, String current) {
  if (json['application'] != 'ir.hamtavar.asandownload2' ||
      json['platform'] != 'windows-x64' || json['channel'] != 'preview') return null;
  final version = json['version'];
  if (version is! String || compareAsanVersions(version, current) <= 0) return null;
  final notes = json['notes'];
  return VersionInfo(version, notes is String ? notes : 'نسخهٔ جدید آسان دانلود آماده است.');
}

Future<VersionInfo?> checkUpdate() async {
  try {
    final client = Dio(BaseOptions(connectTimeout: const Duration(seconds: 5),
      receiveTimeout: const Duration(seconds: 8), responseType: ResponseType.plain));
    try {
      final response = await client.get<String>('https://downloader.hamtavar.ir/asan-release.json');
      return parseAsanRelease(jsonDecode(response.data ?? '') as Map<String, dynamic>, packageInfo.version);
    } finally { client.close(); }
  } catch (_) { return null; } // Offline startup remains usable.
}

// Installation remains an explicit action. This channel never runs another
// project's updater, or the incompatible signed updater for Asan Download 1.x.
Future<void> showUpdateDialog(BuildContext context, VersionInfo info) => showDialog<void>(
  context: context,
  builder: (context) => AlertDialog(
    title: Text('newVersionTitle'.trParams({'version': info.version})),
    content: ConstrainedBox(constraints: const BoxConstraints(maxWidth: 480, maxHeight: 300),
      child: SingleChildScrollView(child: Text('${info.changeLog}\n\nبرای نصب، بستهٔ جدید را از سایت دریافت و کامل استخراج کنید؛ سپس Install.cmd را اجرا کنید. سابقهٔ دانلودها در پوشهٔ جداگانه حفظ می‌شود.'))),
    actions: [
      TextButton(onPressed: () => Navigator.of(context).pop(), child: Text('newVersionLater'.tr)),
      FilledButton.icon(icon: const Icon(Icons.download_rounded), label: const Text('دریافت نسخهٔ جدید'),
        onPressed: () async {
          await launchUrl(Uri.parse('https://downloader.hamtavar.ir/asan-2.html'), mode: LaunchMode.externalApplication);
          if (context.mounted) Navigator.of(context).pop();
        }),
    ],
  ),
);
