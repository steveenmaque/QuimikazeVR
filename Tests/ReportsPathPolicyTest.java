import com.quimikaze.reports.ReportsPathPolicy;
import java.io.File;
import java.io.IOException;
import java.nio.file.Files;

public final class ReportsPathPolicyTest {
    public static void main(String[] args) throws Exception {
        File root = Files.createTempDirectory("quimikaze-reports-test-").toFile();
        File reports = new File(root, "Informes");
        if (!reports.mkdir()) throw new AssertionError("mkdir");
        String name = "Quimikaze_20261112_030400_123.csv";
        File good = new File(reports, name);
        if (!good.createNewFile()) throw new AssertionError("create");
        int checks = 1;
        if (!ReportsPathPolicy.resolve(root, name).equals(good.getCanonicalFile())) throw new AssertionError("valid path");
        String[] bad = { null, "", "../" + name, "sub/" + name, "\\" + name, "/" + name, "Other_20261112_030400_123.csv", "Quimikaze_20261112_030400_123.csv.exe", "Quimikaze_20261112_030400_123.CSV", "Quimikaze_20261112_030400_124.csv", "Quimikaze_20261112_030400_123.csv%00" };
        for (String value : bad) {
            boolean rejected = false;
            try { ReportsPathPolicy.resolve(root, value); } catch (IOException ex) { rejected = true; }
            if (!rejected) throw new AssertionError("accepted: " + value);
            checks++;
        }
        boolean noStorage = false;
        try { ReportsPathPolicy.resolve(null, name); } catch (IOException ex) { noStorage = true; }
        if (!noStorage) throw new AssertionError("null storage");
        checks++;
        Files.delete(good.toPath()); Files.delete(reports.toPath()); Files.delete(root.toPath());
        System.out.println("PASS ReportsPathPolicy: " + checks + " checks; portable Java, no Android runtime");
    }
}
