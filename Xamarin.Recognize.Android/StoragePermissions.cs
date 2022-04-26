using Android.App;
using AndroidX.Core.App;
using Xamarin.Forms;
using Android;

[assembly: Dependency(typeof(XamarinRecognize.Droid.StoragePermissions))]
namespace XamarinRecognize.Droid
{
    
    internal class StoragePermissions : IPermissions
    {
        string[] StoragePermissionsList => new string[]
        {
            Manifest.Permission.ManageExternalStorage,
            Manifest.Permission.WriteExternalStorage,
            Manifest.Permission.ReadExternalStorage
        };

        Activity activity;
        public void RequestPermissions()
        {
            if (activity == null) activity = (Activity)Forms.Context;
            activity.RequestPermissions(StoragePermissionsList, 0);
            ActivityCompat.RequestPermissions(activity, StoragePermissionsList, 1);
            for (int i=0; i< StoragePermissionsList.Length; i++)
            {
                string Permission = StoragePermissionsList[i];
                ActivityCompat.RequestPermissions(activity, new string[] { Permission }, 1);
            }
        }
    }
}