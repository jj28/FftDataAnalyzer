# WPF FFT Application — Design & Implementation Plan

> Target platform: **WPF (Windows only)**, **.NET Framework 4.8** (desktop). Backend and UI in the same WPF app (MVVM). Postgres for persistent data storage. Production-ready, performant, and maintainable.

---

## Quick answers to your initial questions

1. **How to get sample data?**
   - Typical vibration / FFT input is a time-domain signal of samples collected at a known sampling rate (e.g. `SampleRate = 25600 Hz`). Each sample is typically a signed numeric (float/double). A CSV format commonly used:

     ```csv
     Time,Amplitude
     0.000000,0.00123
     0.000039,0.00245
     0.000078,0.00321
     ...
     ```

   - Or a two-column CSV with `Hz` and `Amplitude` if you already have frequency-domain samples. For raw FFT conversion you normally want time-series samples with the sampling frequency.

   - **Small example (time-domain)**

     ```csv
     SampleIndex,Amplitude
     0,0.0000
     1,0.0628
     2,0.1253
     3,0.1868
     4,0.2471
     5,0.3048
     6,0.3588
     7,0.4075
     8,0.4497
     9,0.4836
     10,0.5079
     ```

   - **Small example (already frequency data)**

     ```csv
     FrequencyHz,Amplitude
     0,0.12
     10,0.45
     20,2.34
     30,1.20
     40,0.51
     ```

2. **Which runtime to use?**
   - Use **.NET Framework 4.8** for this WPF app if your target environment is Windows desktops and you specifically require .NET Framework 4.8 compatibility. There is no ".NET Core 4.8" — .NET Core evolved into what Microsoft now calls **.NET 5/6/7/8...** while **.NET Framework** latest major is **4.8**. If you want cross-platform or to use the newest tooling, consider migrating to modern .NET (for WPF you can use .NET 6/7/8), but since you specified .NET Framework 4.8 we will target that.

   - (Source: Microsoft docs and community notes on .NET vs .NET Framework.)

3. **How to generate the FFT chart (production ready, interactive)?**
   - Use a proven plotting library that supports WPF, large datasets, and interactive zoom/pan/scroll. There are two tiers:
     - **Open-source / free & very good**: **ScottPlot** (interactive, supports very large datasets and common mouse interactions). Good balance of performance and ease of use. Also **OxyPlot** is lightweight but sometimes less feature-rich for heavy real-time scenarios. (See *ScottPlot* demos and docs.)
     - **Commercial / highest performance**: **SciChart** or **LightningChart** — extremely fast and GPU-accelerated for real-time streams and huge point counts (paid licenses). Use when you need multi-threaded streaming with millions of points and guaranteed low-latency performance.

   - For this project I recommend **ScottPlot** for an open-source production-quality solution or **SciChart** for a paid-high performance alternative (if budget permits). Both provide mouse-wheel zooming, click-drag pan, and reasonable performance for FFT-sized data.

   - (Sources: ScottPlot site + demos, OxyPlot docs, SciChart docs and community discussion.)

---

## High-level architecture

```
WPF MVVM Client (.NET Framework 4.8)
├─ UI (Views)            -> MainWindow, HomePage (table), FFTDetailPage, UploadDialog, SettingsPage
├─ ViewModels            -> MainWindowVM, HomeVM, FFTDetailVM, UploadVM, AuthVM
├─ Services              -> IFFTService, IFileService, IDbService, IAuthService, IApiClient
├─ Models                -> FftRecord, FftSample, User, ApiConfig
├─ Helpers/Utils         -> CsvParser, Logger, Mapper, ConfigReader
├─ DataAccess            -> Postgres (Npgsql) repository layer
├─ FFT Engine            -> MathNet.Numerics (FFT calculations)
└─ Charting              -> ScottPlot (WPF control) or SciChart (if commercial)

File storage:
C:\FFT\Upload\Success\  (saved uploaded files, images)
C:\FFT\Upload\Fail\

DB: PostgreSQL (hosted locally or network)
Tables: fft_records, fft_samples, users, api_settings, logs
```

---

## UI & UX Design (wireframe + behavior)

### Main layout
- **Left**: Side menu (collapsible) — icons + labels
  - Home
  - Upload
  - Management (API settings)
  - Logs
  - Help / About
- **Top**: Header bar
  - App title / logo at left
  - Breadcrumb / page title center
  - User avatar + username + Logout button at right
- **Content area**: Center-right area for pages

### Home Page (table list)
- Table columns: `ID | DisplayName | CreatedAt | SampleRate(Hz) | SampleCount | PeakFrequency | Actions (View, Delete)`
- Sorting: Click headers to sort asc/desc
- Pagination control: dropdown to choose rows per page (5, 20, 50, 100)
- Search filter (by name, date range)
- Actions column: View -> opens FFT Detail page

### FFT Data Detail Page
- Top: Metadata panel (DisplayName, CreatedAt, SampleRate, SampleCount, UploadedBy, SourceFile)
- Left: small controls — Show raw/time-domain toggle, windowing options (Hann/Hamming/None), FFT length (auto or power-of-two), linear/log scale.
- Right/Center: Interactive Chart area (ScottPlot or SciChart) showing frequency vs amplitude
  - Mouse wheel zoom centered at cursor
  - Click-drag pan
  - Hover tooltip shows (frequency, amplitude)
  - Range selection rectangle to zoom-in; double-click to reset
  - Horizontal scrollbar for quick navigation for extremely wide ranges
- Bottom: table of peak frequencies (Top N peaks) and a button `Export CSV` / `Export PNG`

### Upload workflow
- Drag & drop area or file picker
- Provide Display Name textbox
- Choose Sample Rate (Hz) or try auto-detect
- Upload -> processing status (progress bar + live log)
- After processing success -> stored in DB and file moved to `C:\FFT\Upload\Success\` with timestamped filename. On failure moved to Fail with an error log.

### Management / Settings
- API endpoint settings (base URL, timeouts, auth token)
- Database connection test button
- App-level settings (storage paths, retention policy)

---

## Database schema (Postgres) — core tables

```sql
CREATE TABLE fft_records (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  display_name text NOT NULL,
  source_filename text,
  sample_rate integer NOT NULL,
  sample_count integer NOT NULL,
  created_at timestamptz DEFAULT now(),
  created_by text,
  status text,
  notes text
);

CREATE TABLE fft_samples (
  id bigserial PRIMARY KEY,
  fft_record_id uuid REFERENCES fft_records(id) ON DELETE CASCADE,
  frequency double precision NOT NULL,
  amplitude double precision NOT NULL,
  sample_index integer NOT NULL
);

-- index for fast retrieval
CREATE INDEX idx_fft_record_id ON fft_samples(fft_record_id);
```

> Note: For large datasets store the *time-domain* waveform compressed (binary blob) or in file storage and only persist the frequency-band summary to the DB. Storing millions of per-sample rows in Postgres will become slow — use summarized buckets or store raw waveform as compressed binary files and store metadata & aggregated frequency samples in DB.

---

## Backend design (Core classes / responsibilities)

- **Models**
  - `FftRecord` (id, displayName, sourceFilename, sampleRate, sampleCount, createdAt, status)
  - `FftSample` (id, recordId, frequencyHz, amplitude)

- **Services / Interfaces**
  - `IFFTService`
    - `FftResult ComputeFft(double[] samples, int sampleRate, FftOptions options)`
  - `IFileService`
    - Save uploaded file to staging, move to success/fail.
  - `IDbService` / `IRepository<T>`
    - Insert record, batch insert samples, query records, query samples by record.
  - `IAuthService` — handles simple local auth or API token storage.

- **Flow**
  1. User uploads file -> `UploadVM` calls `IFileService.SaveStaging()`
  2. `UploadService` (background thread/task) parses file (CSV or binary), extracts time-domain samples and sample rate
  3. `IFFTService.ComputeFft` called (use MathNet.Numerics)
  4. On success: store summary samples to DB (or write full frequency CSV file and store pointer), move original file to Success folder.
  5. On fail: move to Fail folder and store error log.

---

## FFT calculation (detailed)

**Library**: MathNet.Numerics — well-tested numeric library for .NET with FFT support and compatible with .NET Framework 4.8.

**Algorithm (overview)**:
1. Read time-domain samples into a `double[]` (length N). If N is not a power of two, choose an FFTLength `M` as next power-of-two >= N or zero-pad/truncate depending on configuration.
2. Optionally apply a window (Hann, Hamming) to reduce spectral leakage.
3. Convert to complex array and call MathNet's FFT routine.
4. Compute magnitudes = `sqrt(re*re + im*im)` and convert to amplitude or dB as needed: `20 * log10(magnitude)`.
5. Compute frequency axis: for bin `k` (0..M-1): `freq = k * sampleRate / M`. For real input you usually show only the first `M/2` bins (positive frequencies).

**Example C# (FFT computation snippet)**

```csharp
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.Window;
using System.Numerics;

public class FftService : IFFTService
{
    public FftResult ComputeFft(double[] samples, int sampleRate, FftOptions options)
    {
        int n = samples.Length;
        int m = NextPowerOfTwo(options.FftLength ?? n);
        // copy and zero-pad
        var complex = new Complex[m];
        for(int i=0;i<n && i<m;i++) complex[i] = new Complex(samples[i], 0);
        // apply window
        var window = Window.Hann(m);
        for(int i=0;i<m;i++) complex[i] *= window[i];
        // FFT in-place
        Fourier.Forward(complex, FourierOptions.Matlab);
        // magnitudes and frequency
        int half = m/2;
        var freqs = new double[half];
        var amps = new double[half];
        for(int k=0;k<half;k++)
        {
            freqs[k] = k * (double)sampleRate / m;
            amps[k] = complex[k].Magnitude;
        }
        return new FftResult { Frequencies = freqs, Amplitudes = amps };
    }
}
```

> Important: choose `FourierOptions` and normalization depending on whether you want power spectral density, amplitude spectrum, or dB scale.

> Sources: MathNet.Numerics docs and community examples.

---

## WPF Implementation details (MVVM)

### NuGet packages (suggested)
- `MathNet.Numerics` (FFT)
- `Npgsql` (Postgres driver)
- `Dapper` or `EntityFramework` (lightweight choose Dapper for performance & control)
- `ScottPlot.WPF` or `SciChart` (chart control)
- `Newtonsoft.Json` (config and API payloads)
- `NLog` or `Serilog` (logging)

### Folder structure (project)
```
/Src
  /Views
  /ViewModels
  /Models
  /Services
  /Data (repositories)
  /Helpers
  /Resources
  /Controls (custom user controls: ChartHost, FileDropArea)
  App.xaml
  MainWindow.xaml

/Test
  Unit tests for Services & FFT engine
```

### Example XAML — MainWindow skeleton

```xml
<Window x:Class="FftApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="FFT Studio" Height="900" Width="1400">
    <DockPanel>
        <!-- Header -->
        <Border DockPanel.Dock="Top" Height="60" Background="#0f172a">
            <!-- Title and logout -->
        </Border>

        <!-- Left side menu -->
        <StackPanel DockPanel.Dock="Left" Width="240" Background="#0b1220">
            <!-- menu items -->
        </StackPanel>

        <!-- Content -->
        <ContentControl Content="{Binding CurrentPage}" />
    </DockPanel>
</Window>
```

### Example ViewModel for Detail page (snippet)

```csharp
public class FftDetailViewModel : ViewModelBase
{
    private readonly IFFTService _fftService;
    private readonly IDbService _db;

    public ObservableCollection<DataPoint> ChartData { get; } = new ObservableCollection<DataPoint>();

    public async Task LoadRecord(Guid id)
    {
        var rec = await _db.GetFftRecord(id);
        var samples = await _db.GetRawTimeDomainSamples(rec); // or load file
        var result = _fftService.ComputeFft(samples, rec.SampleRate, new FftOptions{FftLength=0});
        // populate ChartData
        for(int i=0;i<result.Frequencies.Length;i++)
        {
            ChartData.Add(new DataPoint(result.Frequencies[i], result.Amplitudes[i]));
        }
    }
}
```

### Chart integration (ScottPlot quick example)

```xml
<!-- In FFTDetailPage.xaml -->
<Window xmlns:wpf="clr-namespace:ScottPlot.WPF;assembly=ScottPlot.WPF">
  <wpf:WpfPlot Name="wpfPlot" />
</Window>
```

```csharp
// In code-behind or ViewModel binding helper
wpfPlot.Plot.AddSignal(yValues, sampleRate: sampleRate); // or AddScatter for (freq,amp)
wpfPlot.Refresh();
// ScottPlot supports mouse-wheel zoom, panning and tooltips out-of-the-box.
```

---

## File upload & processing pipeline

1. **UI**: user drag/drop or chooses file and provides DisplayName + SampleRate (if unknown)
2. Save file to `C:\FFT\Upload\Staging\<guid>_<filename>`
3. Start background task (Task.Run or background worker) to process file:
   - Validate CSV or binary format
   - Parse samples and sample rate
   - If parsing fails: move file to `Fail` and store error details
   - If success: compute FFT (IFFTService)
   - Save result: store summary samples in `fft_samples` (or save full CSV to `C:\FFT\Upload\Processed\` and insert pointer in DB)
   - Move original file to `Success` and write a JSON metadata file next to it

> Note: Use robust exception handling and keep processing in an async-safe background queue. Persist processing status to DB to resume after app restart.

---

## Security & Auth

- Minimal local auth: store hashed password (PBKDF2 / bcrypt) for local user accounts in DB. Use HTTPS when calling external APIs. Keep API keys encrypted in config using Windows DPAPI or protected storage.
- Logout button in header clears session and returns to Login page.

---

## Testing & Performance

- Unit tests for: CSV parser, FFT computation (compare against known sine waves), DB repository methods.
- Performance testing: feed large arrays (100k+ samples) and ensure FFT completes in acceptable time. For large sample sizes recommend using streaming/segmented FFTs or down-sample before display.
- For high-frequency real-time streaming, consider a commercial charting library (SciChart) for GPU-acceleration.

---

## Production checklist

- Installer (MSI or setup) that ensures .NET Framework 4.8 is present or installs it.
- Configurable storage paths and retention policy.
- Logging and error reporting (NLog/Serilog local files + optional remote collector)
- Backup plan for DB and raw files.
- Unit tests and CI pipeline (GitHub Actions or Azure DevOps).

---

## Example code snippets (put in project)

1. **CSV parser (simple)**

```csharp
public double[] ParseCsvSamples(string path)
{
    var lines = File.ReadAllLines(path);
    var list = new List<double>();
    foreach(var line in lines.Skip(1))
    {
        var parts = line.Split(',');
        if(parts.Length < 2) continue;
        if(double.TryParse(parts[1], out var v)) list.Add(v);
    }
    return list.ToArray();
}
```

2. **FFT compute wrapper** — (see earlier `FftService` snippet)

3. **Save aggregated results to DB with Dapper**

```csharp
using(var conn = new NpgsqlConnection(connStr)){
  conn.Open();
  using(var tx = conn.BeginTransaction()){
     var id = Guid.NewGuid();
     conn.Execute("INSERT INTO fft_records (id, display_name, sample_rate, sample_count) VALUES (@Id,@Name,@Rate,@Count)", new{Id = id, Name = name, Rate = rate, Count = samples.Length}, tx);
     // batch insert frequencies
     var table = new DataTable();
     table.Columns.Add("fft_record_id", typeof(Guid));
     table.Columns.Add("frequency", typeof(double));
     table.Columns.Add("amplitude", typeof(double));
     // fill table and use Postgres COPY or Dapper's bulk approaches
     tx.Commit();
  }
}
```

---

## Deployment & installer notes
- Use an installer that: ensures Postgres is reachable, installs .NET Framework 4.8 if missing (or show a friendly message), creates the `C:\FFT\Upload\` folders, provides configuration file at `C:\ProgramData\FFTStudio\appsettings.json`.

---

## Next steps / Implementation plan (milestones)

1. Project skeleton (1 day)
   - Create WPF MVVM solution, base ViewModel, DI container, logging
2. File upload UI + parser (1–2 days)
   - Implement drag/drop, staging folder, basic parsing
3. FFT engine + unit tests (1–2 days)
   - Implement MathNet-based FFT and tests using synthetic sine waves
4. DB integration (1–2 days)
   - Create schema, repository, store results
5. Chart integration (ScottPlot) + detail page (1–2 days)
   - Interactive zoom/pan, tooltips, export
6. Settings & auth + management page (1 day)
7. Polish UI, icons, theming + accessibility (1–2 days)
8. Performance tuning & packaging (1–2 days)

> Total rough estimate: 8–13 work-days for an MVP (single-developer focused). (This is a development plan — I have not started asynchronous background tasks; implement now in the codebase.)

---

## References (libraries & docs)
- MathNet.Numerics (FFT): Math.NET website and NuGet.
- ScottPlot: interactive plotting for .NET (demo & docs).
- OxyPlot: lightweight plotting library for WPF.
- SciChart / LightningChart: commercial high-performance options when needed.


---

If you'd like, I can now:

- Generate the **full markdown file** (this document) as a downloadable `.md` (I've created this document here for you).
- Produce a starter Visual Studio solution with sample code files (skeleton) you can open — unit-tested and wired to Postgres stub.


*End of plan.*

