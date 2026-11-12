package com.quimikaze.reports;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.content.Context;
import android.content.pm.ProviderInfo;
import android.database.Cursor;
import android.database.MatrixCursor;
import android.net.Uri;
import android.os.ParcelFileDescriptor;
import android.provider.OpenableColumns;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

/** A single-file read-only provider; Android enforces the temporary URI grant. */
public final class ReportsProvider extends ContentProvider {
    @Override public void attachInfo(Context context, ProviderInfo info) {
        if (info.exported || !info.grantUriPermissions || !info.authority.equals(context.getPackageName() + ".reports"))
            throw new SecurityException("Configuracion insegura del proveedor de informes");
        super.attachInfo(context, info);
    }
    @Override public boolean onCreate() { return true; }

    private File resolve(Uri uri) throws FileNotFoundException {
        List<String> parts = uri.getPathSegments();
        if (!"content".equals(uri.getScheme()) || !(getContext().getPackageName() + ".reports").equals(uri.getAuthority())
            || uri.getQuery() != null || uri.getFragment() != null || parts.size() != 2 || !"reports".equals(parts.get(0)))
            throw new FileNotFoundException("URI de informe no permitida");
        try { return ReportsPathPolicy.resolve(getContext().getExternalFilesDir(null), parts.get(1)); }
        catch (IOException ex) { throw new FileNotFoundException("Informe no encontrado o fuera del directorio permitido"); }
    }
    @Override public ParcelFileDescriptor openFile(Uri uri, String mode) throws FileNotFoundException {
        if (!"r".equals(mode)) throw new FileNotFoundException("Solo se permite lectura");
        return ParcelFileDescriptor.open(resolve(uri), ParcelFileDescriptor.MODE_READ_ONLY);
    }
    @Override public String getType(Uri uri) {
        try { resolve(uri); return "text/csv"; }
        catch (FileNotFoundException ex) { throw new IllegalArgumentException("Informe no permitido", ex); }
    }
    @Override public Cursor query(Uri uri, String[] projection, String selection, String[] selectionArgs, String sortOrder) {
        final File file;
        try { file = resolve(uri); }
        catch (FileNotFoundException ex) { throw new IllegalArgumentException("Informe no permitido", ex); }
        String[] requested = projection == null ? new String[] { OpenableColumns.DISPLAY_NAME, OpenableColumns.SIZE } : projection;
        ArrayList<String> columns = new ArrayList<>();
        ArrayList<Object> values = new ArrayList<>();
        for (String column : requested) {
            if (OpenableColumns.DISPLAY_NAME.equals(column)) { columns.add(column); values.add(file.getName()); }
            else if (OpenableColumns.SIZE.equals(column)) { columns.add(column); values.add(file.length()); }
        }
        MatrixCursor cursor = new MatrixCursor(columns.toArray(new String[0]), 1);
        cursor.addRow(values.toArray());
        return cursor;
    }
    @Override public Uri insert(Uri uri, ContentValues values) { throw new UnsupportedOperationException("Solo lectura"); }
    @Override public int update(Uri uri, ContentValues values, String selection, String[] args) { throw new UnsupportedOperationException("Solo lectura"); }
    @Override public int delete(Uri uri, String selection, String[] args) { throw new UnsupportedOperationException("Solo lectura"); }
}
