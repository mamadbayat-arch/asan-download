import 'package:flutter/material.dart';


final Map<IconData, List<String>> iconConfigMap = {
  Icons.install_desktop_rounded: ['exe', 'msi', 'dmg', 'deb', 'rpm'],
  Icons.android_rounded: ['apk'],
  Icons.phone_iphone_rounded: ['ipa'],
  Icons.hub_outlined: ['torrent'],
  Icons.album_outlined: ['iso'],
  Icons.language_rounded: ['html', 'htm'],
  Icons.description_outlined: ['txt', 'md', 'log', 'csv', 'tsv', 'json', 'yaml', 'yml'],
  Icons.picture_as_pdf_outlined: ['pdf'],
  Icons.article_outlined: ['doc', 'docx'],
  Icons.table_chart_outlined: ['xls', 'xlsx'],
  Icons.slideshow_rounded: ['ppt', 'pptx'],
  Icons.folder_zip_outlined: ['zip', 'rar', '7z', 'tar', 'gz', 'bz2', 'xz'],
  Icons.image_outlined: [
    'jpg',
    'jpeg',
    'png',
    'gif',
    'bmp',
    'tiff',
    'svg',
    'webp'
  ],
  Icons.audio_file_outlined: ['mp3', 'wav', 'flac', 'aac', 'ogg', 'wma', 'm4a'],
  Icons.video_file_outlined: ['mp4', 'avi', 'mkv', 'mov', 'wmv', 'flv', 'webm'],
  Icons.code_rounded: [
    'js',
    'css',
    'json',
    'xml',
    'java',
    'cpp',
    'dart',
    'py',
    'rb',
    'php',
    'ts',
    'swift',
    'go',
    'rs'
  ],
  Icons.insert_drive_file_outlined: [''],
};

final Map<String, IconData> _iconCache = Map.fromEntries(
  iconConfigMap.entries.expand(
    (entry) => entry.value.map((ext) => MapEntry(ext, entry.key)),
  ),
);

String fileExt(String? name) {
  if (name == null) {
    return '';
  }

  final ext = name.split('.').last;
  if (ext.length > 8) {
    return '';
  }

  return ext.toLowerCase();
}

IconData fileIcon(String? name,
    {bool isFolder = false, bool isBitTorrent = false}) {
  if (isFolder) {
    return isBitTorrent ? Icons.folder_shared_outlined : Icons.folder_outlined;
  }

  final ext = fileExt(name);
  return _iconCache[ext] ?? Icons.insert_drive_file_outlined;
}
