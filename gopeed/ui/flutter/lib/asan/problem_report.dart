// SPDX-License-Identifier: GPL-3.0-or-later
import 'dart:math';import 'package:dio/dio.dart';import 'package:flutter/material.dart';
class AsanProblemReport extends StatefulWidget{const AsanProblemReport({super.key});@override State<AsanProblemReport> createState()=>_ReportState();}
class _ReportState extends State<AsanProblemReport>{final title=TextEditingController(),description=TextEditingController(),contact=TextEditingController();bool busy=false;String? status;
 @override void dispose(){title.dispose();description.dispose();contact.dispose();super.dispose();}
 Future<void> send()async{if(title.text.trim().isEmpty||description.text.trim().isEmpty){setState(()=>status='عنوان و توضیح را وارد کنید.');return;}setState((){busy=true;status=null;});
  final client=Dio(BaseOptions(connectTimeout:const Duration(seconds:8),receiveTimeout:const Duration(seconds:8)));
  try{final random=Random.secure(),id=List.generate(16,(_)=>random.nextInt(256).toRadixString(16).padLeft(2,'0')).join();await client.post('https://downloader.hamtavar.ir/api/public/reports',data:{'id':id,'title':title.text.trim(),'description':description.text.trim(),'contact':contact.text.trim(),'category':'دانلود','version':'2.0.1'});if(mounted){setState(()=>status='گزارش ثبت شد.');}}
  catch(_){if(mounted)setState(()=>status='ارسال نشد؛ اتصال اینترنت را بررسی و دوباره تلاش کنید.');}finally{client.close();if(mounted)setState(()=>busy=false);}
 }
 @override Widget build(BuildContext context)=>Directionality(textDirection:TextDirection.rtl,child:AlertDialog(title:const Text('گزارش مشکل'),content:SizedBox(width:440,child:SingleChildScrollView(child:Column(mainAxisSize:MainAxisSize.min,children:[TextField(controller:title,maxLength:120,decoration:const InputDecoration(labelText:'عنوان')),TextField(controller:description,maxLength:4000,minLines:3,maxLines:6,decoration:const InputDecoration(labelText:'توضیح مشکل')),TextField(controller:contact,maxLength:160,decoration:const InputDecoration(labelText:'راه ارتباطی (اختیاری)')),if(status!=null)Text(status!)]))),actions:[TextButton(onPressed:()=>Navigator.of(context).pop(),child:const Text('بستن')),FilledButton(onPressed:busy?null:send,child:Text(busy?'در حال ارسال…':'ارسال گزارش'))]));
}
