using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;

namespace TimeLauncher
{
    [Activity(Label = "AppSelectActivity")]
    public class AppSelectActivity : Activity
    {
        List<AppInfo> tableItems = new List<AppInfo>();
        AppInfo selectedAppInfo;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.AppSelect);

            // アプリ一覧を取得 参考：
            // http://furuya02.hatenablog.com/entry/20140528/1401224919
            var mainIntent = new Intent(Intent.ActionMain, null);
            mainIntent.AddCategory(Intent.CategoryLauncher);
            var ar = PackageManager.QueryIntentActivities(mainIntent, 0);
            foreach(var a in ar)
            {
                tableItems.Add(new AppInfo()
                {
                    Icon = a.ActivityInfo.LoadIcon(PackageManager),
                    LoadLabel = a.LoadLabel(PackageManager),
                    ClassName = a.ActivityInfo.Name,
                    PackageName = a.ActivityInfo.PackageName
                });
            }

            // アプリ一覧をリストに表示
            var listview = FindViewById<ListView>(Resource.Id.list_App);
            listview.Adapter = new AppSelectAdapter(this, tableItems);
            listview.ItemClick += Listview_ItemClick;
        }

        private void Listview_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            // 選択したアプリの情報を呼出元に返す
            selectedAppInfo = tableItems[e.Position];
            
            Intent intent = new Intent();
            intent.PutExtra("EXTRA_LABEL", selectedAppInfo.LoadLabel);
            intent.PutExtra("EXTRA_PKGNAME", selectedAppInfo.PackageName);
            intent.PutExtra("EXTRA_CLSNAME", selectedAppInfo.ClassName);
            SetResult(Result.Ok, intent);
            Finish();
        }

    }

    public class AppInfo
    {
        public string LoadLabel { get; set; }
        public string ClassName { get; set; }
        public string PackageName { get; set; }
        public Drawable Icon { get; set; }
    }

    public class AppSelectAdapter : BaseAdapter<AppInfo>
    {
        List<AppInfo> items;
        Activity context;

        public AppSelectAdapter(Activity context, List<AppInfo> items) : base()
        {
            this.context = context;
            this.items = items;
        }

        public override AppInfo this[int position]
        {
            get { return items[position];  }
        }

        public override int Count
        {
            get { return items.Count; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = items[position];
            View view = context.LayoutInflater.Inflate(Android.Resource.Layout.ActivityListItem, null);
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = item.LoadLabel;
            view.FindViewById<ImageView>(Android.Resource.Id.Icon).SetImageDrawable(item.Icon);
            return view;
        }
    }
}