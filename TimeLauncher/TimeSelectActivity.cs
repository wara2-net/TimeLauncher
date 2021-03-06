﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using System;

namespace TimeLauncher
{
    [Activity(Label = "TimeSelectActivity")]
    public class TimeSelectActivity : Activity
    {
        private string displayString;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here            
            SetContentView(Resource.Layout.TimeSelect);

            // コントロールをGET
            TimePicker timePicker = FindViewById<TimePicker>(Resource.Id.timePicker1);
            Button button = FindViewById<Button>(Resource.Id.btn_setTime);
            EditText txtSmtp = FindViewById<EditText>(Resource.Id.txtSmtp);
            EditText txtPass = FindViewById<EditText>(Resource.Id.txtPass);
            EditText txtPort = FindViewById<EditText>(Resource.Id.txtPort);
            EditText txtAccount = FindViewById<EditText>(Resource.Id.txtAccount);
            EditText txtMailTo = FindViewById<EditText>(Resource.Id.txtMailTo);


            //前の画面から受け取った時刻文字列をTimePickerに表示
            Intent intent = Intent;
            displayString = intent.GetStringExtra("EXTRA_DATA");
            timePicker.Hour = int.Parse(displayString.Split(":")[0]);
            timePicker.Minute = int.Parse(displayString.Split(":")[1]);
            txtSmtp.Text = intent.GetStringExtra("EXTRA_SMTP");
            txtPort.Text = intent.GetStringExtra("EXTRA_PORT");
            txtPass.Text = intent.GetStringExtra("EXTRA_PASS");
            txtAccount.Text = intent.GetStringExtra("EXTRA_ACCOUNT");
            txtMailTo.Text = intent.GetStringExtra("EXTRA_MAILTO");


            // ボタンのイベントハンドラ
            button.Click += Button_Click; 
        }
        
        private void Button_Click(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            // TimePickerで指定した時刻を呼び出し元に戻す
            TimePicker timePicker = FindViewById<TimePicker>(Resource.Id.timePicker1);
            displayString = timePicker.Hour + ":" + timePicker.Minute;

            EditText txtSmtp = FindViewById<EditText>(Resource.Id.txtSmtp);
            EditText txtPass = FindViewById<EditText>(Resource.Id.txtPass);
            EditText txtPort = FindViewById<EditText>(Resource.Id.txtPort);
            EditText txtAccount = FindViewById<EditText>(Resource.Id.txtAccount);
            EditText txtMailTo = FindViewById<EditText>(Resource.Id.txtMailTo);


            Intent intent = new Intent();
            intent.PutExtra("EXTRA_DATA", displayString);
            intent.PutExtra("EXTRA_SMTP", txtSmtp.Text);
            intent.PutExtra("EXTRA_PASS", txtPass.Text);
            intent.PutExtra("EXTRA_PORT", txtPort.Text);
            intent.PutExtra("EXTRA_ACCOUNT", txtAccount.Text);
            intent.PutExtra("EXTRA_MAILTO", txtMailTo.Text);
            SetResult(Result.Ok, intent);
            Finish();
        }
    }
}