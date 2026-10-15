// SPDX-License-Identifier: GPL-3.0-or-later
import 'dart:convert';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:open_filex/open_filex.dart';
import 'package:path/path.dart' as p;
import 'package:shamsi_date/shamsi_date.dart';

class LegacyRecord {
 LegacyRecord(this.data);
 final Map<String,dynamic> data;
 String get path=>data['Path'] as String? ?? '';
 String get name=>p.basename(path);
 String get key=>data['Id'] as String? ?? '$path|${data['Finished']}';
 DateTime? get finished {
  final value=data['Finished']?.toString()??'';
  final epoch=RegExp(r'/Date\((-?\d+)').firstMatch(value);
  return epoch==null?DateTime.tryParse(value)?.toLocal():DateTime.fromMillisecondsSinceEpoch(int.parse(epoch.group(1)!)).toLocal();
 }
 String get date {
  final value=finished;if(value==null)return '—';
  try{final j=Jalali.fromDateTime(value);return '${j.year}/${j.month.toString().padLeft(2,'0')}/${j.day.toString().padLeft(2,'0')} ${value.hour.toString().padLeft(2,'0')}:${value.minute.toString().padLeft(2,'0')}';}catch(_){return '—';}
 }
 String get size=>'${((data['Size'] as num? ?? 0)/1048576).toStringAsFixed(2)} MB';
 String get duration {final seconds=(data['DurationSeconds'] as num?)?.ceil();if(seconds==null)return '—';return '${(seconds~/3600).toString().padLeft(2,'0')}:${((seconds~/60)%60).toString().padLeft(2,'0')}:${(seconds%60).toString().padLeft(2,'0')}';}
 String get average {final rate=data['AverageBytesPerSecond'] as num?;return rate==null?'—':'${(rate/1000000).toStringAsFixed(2)} MB/s';}
}
Future<List<LegacyRecord>> readLegacyHistory(String root) async {
 final records=<String,LegacyRecord>{};
 void add(dynamic data){if(data is! Map)return;final row=LegacyRecord(Map<String,dynamic>.from(data));if(row.path.isNotEmpty)records[row.key]=row;}
 for(final name in ['session.json.bak','session.json']){try{final data=jsonDecode(await File(p.join(root,name)).readAsString());for(final row in data['History'] as List? ?? []){add(row);}}catch(_){}}
 final journal=Directory(p.join(root,'session.json.history'));
 if(await journal.exists()){await for(final file in journal.list()){if(file is File&&file.path.endsWith('.json')){try{add(jsonDecode(await file.readAsString()));}catch(_){}}}}
 final result=records.values.toList();result.sort((a,b)=>(b.finished??DateTime(1900)).compareTo(a.finished??DateTime(1900)));return result;
}
class LegacyHistoryPage extends StatelessWidget {
 const LegacyHistoryPage({super.key});
 Future<List<LegacyRecord>> _load()=>readLegacyHistory(p.join(Platform.environment['LOCALAPPDATA']??'','MehrDownload'));
 @override Widget build(BuildContext context)=>Directionality(textDirection:TextDirection.rtl,child:Scaffold(
  appBar:AppBar(title:const Text('دانلودهای نسخهٔ قبلی')),
  body:FutureBuilder<List<LegacyRecord>>(future:_load(),builder:(context,snapshot){
   if(!snapshot.hasData)return const Center(child:CircularProgressIndicator());final rows=snapshot.data!;
   if(rows.isEmpty)return const Center(child:Text('سابقه‌ای از نسخهٔ قبلی پیدا نشد.'));
   return ListView.builder(itemCount:rows.length,itemBuilder:(context,index){final file=rows[index];return ListTile(
    leading:const Icon(Icons.download_done_rounded),title:Text(file.name,textDirection:TextDirection.ltr),
    subtitle:Text('${file.size}  •  ${file.date}\nمدت: ${file.duration}  •  میانگین: ${file.average}'),
    onTap:()async{if(await File(file.path).exists()){await OpenFilex.open(file.path);}else if(context.mounted){ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content:Text('فایل جابه‌جا یا حذف شده؛ سابقه حفظ شده است.')));}},
    trailing:IconButton(tooltip:'نمایش در پوشه',icon:const Icon(Icons.folder_open_rounded),onPressed:()async{if(Platform.isWindows&&await File(file.path).exists())await Process.start('explorer.exe',['/select,',file.path]);}),
   );});
  }),
 ));
}
