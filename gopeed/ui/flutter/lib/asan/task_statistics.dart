// SPDX-License-Identifier: GPL-3.0-or-later
import 'dart:convert';import 'dart:io';
import 'package:flutter/material.dart';import 'package:path/path.dart' as p;import 'package:shamsi_date/shamsi_date.dart';
import '../api/model/task.dart';import '../util/util.dart';
String persianCompletionDate(DateTime date){final local=date.toLocal(),j=Jalali.fromDateTime(local);return '${j.year}/${j.month.toString().padLeft(2,'0')}/${j.day.toString().padLeft(2,'0')} ${local.hour.toString().padLeft(2,'0')}:${local.minute.toString().padLeft(2,'0')}';}
String activeDuration(int nanoseconds){if(nanoseconds<=0)return '—';final s=(nanoseconds/1000000000).ceil();return '${(s~/3600).toString().padLeft(2,'0')}:${((s~/60)%60).toString().padLeft(2,'0')}:${(s%60).toString().padLeft(2,'0')}';}
String formatDownloadSpeed(num bytesPerSecond,bool bits)=>'${(bytesPerSecond*(bits?8:1)/1000000).toStringAsFixed(2)} ${bits?'Mb/s':'MB/s'}';
class SpeedUnits {
 static final bits=ValueNotifier(false);static bool loaded=false;
 static File get file=>File(p.join(Util.getStorageDir(),'asan-ui.json'));
 static Future<void> load()async{if(loaded)return;loaded=true;try{bits.value=jsonDecode(await file.readAsString())['bits']==true;}catch(_){}}
 static Future<void> toggle()async{bits.value=!bits.value;try{await file.parent.create(recursive:true);await file.writeAsString(jsonEncode({'bits':bits.value}),flush:true);}catch(_){}}
}
class AsanSpeed extends StatefulWidget{const AsanSpeed(this.bytes,{super.key});final int bytes;@override State<AsanSpeed> createState()=>_SpeedState();}
class _SpeedState extends State<AsanSpeed>{@override void initState(){super.initState();SpeedUnits.load();}@override Widget build(BuildContext context)=>ValueListenableBuilder<bool>(valueListenable:SpeedUnits.bits,builder:(_,bits,__)=>TextButton(onPressed:SpeedUnits.toggle,child:Text(formatDownloadSpeed(widget.bytes,bits),textDirection:TextDirection.ltr)));}
class TaskStatistics extends StatelessWidget{const TaskStatistics(this.task,{super.key});final Task task;@override Widget build(BuildContext context)=>ValueListenableBuilder<bool>(valueListenable:SpeedUnits.bits,builder:(_,bits,__){final used=task.progress.used;final average=used>0?formatDownloadSpeed(task.progress.downloaded/(used/1000000000),bits):'—';return Padding(padding:const EdgeInsetsDirectional.fromSTEB(16,0,16,10),child:Align(alignment:AlignmentDirectional.centerStart,child:Text('تاریخ: ${persianCompletionDate(task.updatedAt)}  •  مدت: ${activeDuration(used)}  •  میانگین: $average',style:Theme.of(context).textTheme.bodySmall)));});}
