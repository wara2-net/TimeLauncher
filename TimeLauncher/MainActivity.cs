using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Widget;
using System;
using Xamarin.Essentials;

namespace TimeLauncher
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private DateTime displayTime;
        string launchApp_LabelName;
        string launchApp_className;
        string launchApp_pkgName;
        private MyReceiver myReceiver = new MyReceiver();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            // アイコンの取得先 https://www.iconfinder.com/iconsets/moe-moe-icons
            //保存した情報を読み取り、メンバに保存
            var saveTime = this.GetSharedPreferences("app", FileCreationMode.Private);
            launchApp_LabelName = saveTime.GetString("LabelName", "(起動するアプリが選択されていません)");
            launchApp_className = saveTime.GetString("ClassName", "");
            launchApp_pkgName = saveTime.GetString("PkgName", "");
            saveTime = this.GetSharedPreferences("time", FileCreationMode.Private);
            var tmpTime = saveTime.GetString("time", "00:00");
            displayTime = DateTime.Parse(tmpTime);

            // 画面表示の設定
            TextView textView2 = FindViewById<TextView>(Resource.Id.textView2);
            TextView textView1 = FindViewById<TextView>(Resource.Id.textView1);
            ImageView imageView = FindViewById<ImageView>(Resource.Id.appIcon);
            textView2.Text = displayTime.ToShortTimeString() + " 以降に起動可能";
            textView1.Text = launchApp_LabelName;
            imageView.SetImageDrawable(this.getIconfromClassName(launchApp_className));

            // 各種ボタンのイベントハンドラ作成
            var btn_showSelectTime = FindViewById<Button>(Resource.Id.btn_showSelectTime);
            var btn_launch = FindViewById<Button>(Resource.Id.btn_Launch);
            var btn_showSelectApp = FindViewById<Button>(Resource.Id.btn_showSelectApp);
            btn_showSelectTime.Click += btn_showSelectTime_Click;
            btn_showSelectApp.Click += Btn_showSelectApp_Click;
            btn_launch.Click += Btn_launch_Click;
        }

        protected override void OnPause()
        {
            base.OnPause();

            //http://y-anz-m.blogspot.com/2011/08/androidkeygurad.html
            IntentFilter f = new IntentFilter();
            f.AddAction(Intent.ActionUserPresent);
            RegisterReceiver(myReceiver, f);
        }
        protected override void OnResume()
        {
            base.OnResume();

            if (myReceiver.IsOrderedBroadcast)
            {
                UnregisterReceiver(myReceiver);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (myReceiver.IsOrderedBroadcast)
            {
                UnregisterReceiver(myReceiver);
            }
        }


        private void Btn_showSelectApp_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(AppSelectActivity));
            int requestCode = 2000;
            StartActivityForResult(intent, requestCode);
        }

        private void Btn_launch_Click(object sender, EventArgs e)
        {
            // 時刻の比較
            if (DateTime.Now.TimeOfDay < displayTime.TimeOfDay)
            {
                try
                {
                    Vibration.Vibrate();
                }
                catch (FeatureNotSupportedException)
                {
                    // バイブしてもしなくても影響はないので例外を無視
                }
                Toast.MakeText(this, "まだ時間になっていません", ToastLength.Short).Show();
                return;
            }

            // 選択済みのアプリ起動
            if (string.IsNullOrEmpty(launchApp_pkgName)) return;
            var intent = Intent;
            intent.SetClassName(launchApp_pkgName, launchApp_className);
            StartActivity(intent);
        }

        private void btn_showSelectTime_Click(object sender, System.EventArgs e)
        {
            // 遷移先の画面にデータを渡す
            var intent = new Intent(this, typeof(TimeSelectActivity));
            intent.PutExtra("EXTRA_DATA", displayTime.ToString("HH:mm"));
            int requestCode = 1000;
            //StartActivity(intent);
            StartActivityForResult(intent, requestCode);    // 遷移先から値を受け取りたいからこっち
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode != Result.Ok || null == data) return;
            
            // TimePickerから値を受け取り、TextViewに表示する
            if (requestCode == 1000)
            {
                string res = data.GetStringExtra("EXTRA_DATA");
                displayTime = DateTime.Parse(res);
                TextView textView2 = FindViewById<TextView>(Resource.Id.textView2);
                textView2.Text = displayTime.ToShortTimeString() + " 以降に起動可能";

                // 受け取った時刻を保存する
                var saveTime = this.GetSharedPreferences("time", FileCreationMode.Private);
                var editor = saveTime.Edit();
                editor.PutString("time", displayTime.ToShortTimeString());
                editor.Commit();
            }

            // アプリ選択から値を受け取り、画面に表示
            if(requestCode == 2000){
                launchApp_LabelName = data.GetStringExtra("EXTRA_LABEL");
                launchApp_className = data.GetStringExtra("EXTRA_CLSNAME");
                launchApp_pkgName = data.GetStringExtra("EXTRA_PKGNAME");

                TextView textView = FindViewById<TextView>(Resource.Id.textView1);
                textView.Text = launchApp_LabelName;

                ImageView imageView = FindViewById<ImageView>(Resource.Id.appIcon);
                imageView.SetImageDrawable(this.getIconfromClassName(launchApp_className));

                // 値を保存
                var saveAppInfo = this.GetSharedPreferences("app", FileCreationMode.Private);
                var editor = saveAppInfo.Edit();
                editor.PutString("LabelName", launchApp_LabelName);
                editor.PutString("ClassName", launchApp_className);
                editor.PutString("PkgName", launchApp_pkgName);
                editor.Commit();
            }
        }

        /// <summary>
        /// インストール済みアプリのクラス名を渡して、アプリのアイコンのDrawableを取得する
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        private Drawable getIconfromClassName(string className)
        {
            var mainIntent = new Intent(Intent.ActionMain, null);
            mainIntent.AddCategory(Intent.CategoryLauncher);
            var ar = PackageManager.QueryIntentActivities(mainIntent, 0);
            foreach (var a in ar)
            {
                if (a.ActivityInfo.Name == className)
                {
                    return a.ActivityInfo.LoadIcon(PackageManager);
                }
            }
            return null;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

    }


    public class MyReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;

            if(action == Intent.ActionUserPresent)
            {
                //ロック解除イベント発生時、アプリを起動(前面に表示)させる
                Toast.MakeText(context, "ロック解除のイベント検知", ToastLength.Short).Show();
                context.StartActivity(typeof(MainActivity));
            }
        }
    }
}