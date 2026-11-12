package com.quimikaze.reports;

import android.app.Activity;
import android.content.ClipData;
import android.content.Intent;
import android.net.Uri;
import android.widget.Toast;
import java.io.File;
import java.io.IOException;

/** Called through Unity JNI. No account, broad storage permission or external dependency. */
public final class ReportShareBridge {
    private ReportShareBridge() { }

    public static void share(final Activity activity, String absolutePath) throws IOException {
        if (activity == null || activity.isFinishing()) throw new IllegalStateException("Actividad no disponible");
        final File requested = new File(absolutePath).getCanonicalFile();
        final File allowed = ReportsPathPolicy.resolve(activity.getExternalFilesDir(null), requested.getName());
        if (!requested.equals(allowed)) throw new SecurityException("Ruta fuera de Informes");
        final Uri uri = new Uri.Builder().scheme("content").authority(activity.getPackageName() + ".reports")
            .appendPath("reports").appendPath(allowed.getName()).build();
        activity.runOnUiThread(new Runnable() {
            @Override public void run() {
                try {
                    Intent send = new Intent(Intent.ACTION_SEND);
                    send.setType("text/csv");
                    send.putExtra(Intent.EXTRA_STREAM, uri);
                    send.putExtra(Intent.EXTRA_SUBJECT, "Informe de aprendizaje Quimikaze");
                    send.setClipData(ClipData.newRawUri("Informe Quimikaze", uri));
                    send.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                    Intent chooser = Intent.createChooser(send, "Compartir informe CSV");
                    chooser.setClipData(send.getClipData());
                    chooser.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                    activity.startActivity(chooser);
                } catch (RuntimeException ex) {
                    Toast.makeText(activity, "Informe guardado. No se pudo abrir Compartir; vuelve a intentarlo.", Toast.LENGTH_LONG).show();
                }
            }
        });
    }
}
