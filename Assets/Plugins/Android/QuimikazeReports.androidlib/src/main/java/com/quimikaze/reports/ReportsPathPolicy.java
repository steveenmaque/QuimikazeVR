package com.quimikaze.reports;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;

/** Pure Java allowlist, independently executable without an Android runtime. */
public final class ReportsPathPolicy {
    private ReportsPathPolicy() { }

    public static File resolve(File externalFiles, String name) throws IOException {
        if (externalFiles == null || name == null || !name.matches("Quimikaze_[0-9]{8}_[0-9]{6}_[0-9]{3}\\.csv"))
            throw new FileNotFoundException("Informe no permitido");
        File external = externalFiles.getCanonicalFile();
        File reports = new File(external, "Informes").getCanonicalFile();
        if (!external.equals(reports.getParentFile()))
            throw new FileNotFoundException("Directorio no permitido");
        File file = new File(reports, name).getCanonicalFile();
        if (!reports.equals(file.getParentFile()) || !file.getName().equals(name) || !file.isFile())
            throw new FileNotFoundException("Informe no encontrado");
        return file;
    }
}
