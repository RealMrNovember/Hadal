package com.cicibyte.hadal;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.database.Cursor;
import android.net.Uri;
import android.os.ParcelFileDescriptor;

import java.io.File;
import java.io.FileNotFoundException;

/**
 * Read-only access to downloaded update APKs in the app's cache directory, so the system
 * installer can read them (a file:// URI is refused since Android 7). Registered in the manifest
 * by HadalAndroidManifest.cs as "${applicationId}.apkprovider", not exported.
 */
public final class ApkProvider extends ContentProvider {
    public static final String AUTHORITY_SUFFIX = ".apkprovider";
    public static final String MIME = "application/vnd.android.package-archive";

    @Override
    public boolean onCreate() {
        return true;
    }

    @Override
    public String getType(Uri uri) {
        return MIME;
    }

    @Override
    public ParcelFileDescriptor openFile(Uri uri, String mode) throws FileNotFoundException {
        String name = uri.getLastPathSegment();
        if (name == null || !name.endsWith(".apk") || name.contains("/") || name.contains(".."))
            throw new FileNotFoundException(String.valueOf(uri));

        File file = new File(getContext().getCacheDir(), name);
        if (!file.isFile())
            throw new FileNotFoundException(file.getPath());

        return ParcelFileDescriptor.open(file, ParcelFileDescriptor.MODE_READ_ONLY);
    }

    @Override
    public Cursor query(Uri uri, String[] projection, String selection, String[] selectionArgs, String sortOrder) {
        return null;
    }

    @Override
    public Uri insert(Uri uri, ContentValues values) {
        return null;
    }

    @Override
    public int delete(Uri uri, String selection, String[] selectionArgs) {
        return 0;
    }

    @Override
    public int update(Uri uri, ContentValues values, String selection, String[] selectionArgs) {
        return 0;
    }
}
