// SPDX-License-Identifier: GPL-3.0-or-later
import 'dart:async';
import 'package:flutter/material.dart';
import 'package:get/get.dart';

bool validBrowserUrl(String value) {
 final uri=Uri.tryParse(value);
 return value.length<=8192 && uri!=null && ['http','https'].contains(uri.scheme) && uri.host.isNotEmpty && uri.userInfo.isEmpty;
}
Future<bool> confirmBrowserDownload(String url) async {
 final uri=Uri.parse(url);
 final name=uri.pathSegments.isEmpty?uri.host:uri.pathSegments.last;
 var finished=false;
 final timer=Timer(const Duration(seconds:90),(){if(!finished&&Get.isDialogOpen==true)Get.back(result:false);});
 try{return await Get.dialog<bool>(AlertDialog(
  title:const Text('این فایل دانلود شود؟'),
  content:SelectableText('$name\n${uri.host}',textDirection:TextDirection.ltr),
  actions:[TextButton(onPressed:()=>Get.back(result:false),child:const Text('انصراف')),FilledButton(onPressed:()=>Get.back(result:true),child:const Text('دانلود کن'))],
 ),barrierDismissible:false)??false;}finally{finished=true;timer.cancel();}
}
