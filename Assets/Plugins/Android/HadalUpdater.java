package com.cicibyte.hadal;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.provider.Settings;

import java.io.File;

/**
 * In-app (OTA) update for sideloaded builds: hands a downloaded APK from the app's cache to the
 * system installer through {@link ApkProvider}. Framework APIs only (no androidx).
 */
public final class HadalUpdater {
    private HadalUpdater() {
    }

    /** True when this app may open the package installer (Android 8+ asks the player once). */
    public static boolean canInstall(Activity activity) {
        return Build.VERSION.SDK_INT < 26 || activity.getPackageManager().canRequestPackageInstalls();
    }

    /** Opens the "install unknown apps" switch for this app. */
    public static void openInstallPermission(Activity activity) {
        Intent intent = new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES, Uri.parse("package:" + activity.getPackageName()));
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        activity.startActivity(intent);
    }

    /** Starts the system installer for an APK in the cache directory; false when permission is missing. */
    public static boolean install(Activity activity, String path) {
        if (!canInstall(activity)) {
            openInstallPermission(activity);
            return false;
        }

        Uri uri = Uri.parse("content://" + activity.getPackageName() + ApkProvider.AUTHORITY_SUFFIX + "/" + new File(path).getName());
        Intent intent = new Intent(Intent.ACTION_VIEW);
        intent.setDataAndType(uri, ApkProvider.MIME);
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_ACTIVITY_NEW_TASK);
        activity.startActivity(intent);
        return true;
    }
}
