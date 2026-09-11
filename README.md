# WAMS

## Lokasi Repository

```text
C:\...\wams
```

Backend berada di:

```text
C:\...\wams\backend
```

Frontend berada di:

```text
C:\...\wams\frontend
```

## Prasyarat

Pastikan komponen berikut sudah terpasang:

1. GitHub Desktop.
2. .NET SDK 10
3. Node.js 20++.
4. PostgreSQL 17.

Periksa service PostgreSQL melalui PowerShell:

```powershell
Get-Service postgresql-x64-17
```

Status yang benar adalah `Running`.

Pastikan database `wams` sudah tersedia. Password pada `backend\.env` harus
sesuai dengan password PostgreSQL lokal.

## Konfigurasi Backend

File konfigurasi backend harus berada di:

```text
backend\.env
```

Nilai penting yang harus diperiksa:

```env
PORT=8080
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=wams;Username=postgres;Password=PASSWORD_POSTGRES
Jwt__Secret=SECRET_JWT
InitialAdmin__Password=PASSWORD_ADMIN
```

### HTTPS langsung pada port 8121

Jika certificate dari client berupa PFX, tambahkan ke `backend\.env`:

```env
PORT=8121
HTTPS=true
HTTPS_CERT_PATH=C:/WAMS/certificates
# Kosongkan jika file PFX tidak menggunakan password.
HTTPS_CERT_PASSWORD=
CORS__Origins=https://DOMAIN_CLIENT:8120
```

`HTTPS_CERT_PATH` dapat menunjuk langsung ke file `.pfx` atau ke folder yang
berisi tepat satu file `.pfx`. Folder kosong atau folder dengan beberapa file
`.pfx` menghasilkan pesan error yang menjelaskan masalahnya.

`run.ps1` memuat nilai tersebut sebelum menjalankan backend. Jika `HTTPS=false`,
backend tetap menggunakan HTTP seperti sebelumnya.

## Menjalankan Backend

Backend mendukung dua pilihan konfigurasi. Cara lama dengan `.env` tetap
tersedia dan `run.ps1` tidak berubah. Sebagai alternatif, salin template JSON
standar ASP.NET Core:

```powershell
Copy-Item appsettings.Production.example.json src\WAMS.Api\appsettings.Production.json
dotnet run --project src\WAMS.Api --no-launch-profile --environment Production
```

File `src\WAMS.Api\appsettings.Production.json` diabaikan Git karena dapat
berisi secret. Konfigurasinya mengikuti `windows.env` terbaru. Jika environment
variable dan JSON digunakan bersamaan, environment variable memiliki prioritas
lebih tinggi.

1. Buka PowerShell.
2. Masuk ke folder backend:

   ```powershell
   cd C:\...\wams\backend
   ```

3. Jalankan runner:

   ```powershell
   .\run.ps1
   ```

Runner akan melakukan hal berikut secara otomatis:

1. Membaca `backend\.env`.
2. Membuat Release publish ke `backend\publish`.
3. Menjalankan `WAMS.Api.dll` dari folder publish.

Folder `backend\publish` adalah hasil build dan tidak di-commit ke Git.

Biarkan PowerShell tetap terbuka selama backend digunakan.

Periksa health backend melalui browser:

```text
http://localhost:8080/health
```

Untuk menghentikan backend, tekan `Ctrl+C` pada PowerShell backend.

## Konfigurasi Frontend

File konfigurasi frontend harus berada di:

```text
frontend\.env
```

Untuk frontend dan backend yang berjalan pada VM yang sama, gunakan:

```env
FRONTEND_PORT=5173
VITE_API_URL=http://localhost:8080/
VITE_API_URL_TEST=http://localhost:8080/
VITE_WAMS_API_URL=http://localhost:8080/
```

Untuk menjalankan frontend melalui HTTPS pada port 8120 menggunakan PFX yang
sama, gunakan:

```env
FRONTEND_PORT=8120
HTTPS=true
HTTPS_CERT_PATH=C:/WAMS/certificates
# Kosongkan jika file PFX tidak menggunakan password.
HTTPS_CERT_PASSWORD=
VITE_API_URL=https://DOMAIN_CLIENT:8121/
VITE_API_URL_TEST=https://DOMAIN_CLIENT:8121/
VITE_WAMS_API_URL=https://DOMAIN_CLIENT:8121/
```

Hostname pada URL harus tercantum pada SAN certificate. Windows Firewall juga
harus mengizinkan TCP port 8120 dan 8121. Jangan commit file PFX, `.env`, atau
password certificate.

`VITE_API_URL` dibaca saat proses build. Jika nilainya diubah, frontend harus
dibuild ulang.

Jika `FRONTEND_PORT` diubah, `CORS__Origins` pada `backend\.env` juga harus
memuat alamat frontend yang baru.

## Menjalankan Frontend

Backend harus sudah berjalan sebelum frontend digunakan.

1. Buka PowerShell baru.
2. Masuk ke folder frontend:

   ```powershell
   cd C:\...\wams\frontend
   ```

3. Jalankan instalasi dependency pada instalasi pertama atau setelah
   `package-lock.json` berubah:

   ```powershell
   npm ci
   ```

4. Build dan jalankan frontend:

   ```powershell
   npm run prod
   ```

Perintah `npm run prod` akan:

1. Menjalankan `npm run build`.
2. Membuat atau memperbarui folder `frontend\dist`.
3. Menjalankan hasil build pada port yang ditentukan oleh `FRONTEND_PORT`.

Buka frontend melalui browser:

```text
http://localhost:5173
```

Biarkan PowerShell frontend tetap terbuka selama frontend digunakan.

Untuk menghentikan frontend, tekan `Ctrl+C` pada PowerShell frontend.

Perintah `npm start` hanya menjalankan hasil build yang sudah ada. Gunakan
`npm run prod` setelah ada perubahan pada source frontend.

## Urutan Menjalankan Aplikasi

1. Pastikan PostgreSQL berstatus `Running`.
2. Buka PowerShell backend dan jalankan `.\run.ps1`.
3. Tunggu sampai backend aktif pada port `8080`.
4. Buka PowerShell frontend dan jalankan `npm run prod`.
5. Buka `http://localhost:5173` pada browser.

## Setelah Pull Perubahan dari Git

1. Pull perubahan melalui GitHub Desktop.
2. Jika ada perubahan pada backend, jalankan ulang:

   ```powershell
   cd C:\...\wams\backend
   .\run.ps1
   ```

   Runner akan melakukan publish terbaru secara otomatis.

3. Jika ada perubahan pada frontend, jalankan ulang:

   ```powershell
   cd C:\...\wams\frontend
   npm run prod
   ```

4. Jalankan `npm ci` kembali jika `package.json` atau `package-lock.json`
   berubah.

## Background Deployment (Opsional)

Gunakan **Task Scheduler** agar backend dan frontend berjalan otomatis setelah
Windows menyala. Buat dua task dengan opsi General berikut:

```text
Run whether user is logged on or not
Run with highest privileges
```

### Task 1: WAMS Backend

Trigger:

```text
At startup, delay 30 seconds
```

Action:

```text
Program:   C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe
Arguments: -NoProfile -ExecutionPolicy Bypass -File "C:\Users\Administrator\Documents\GitHub\wams\backend\run.ps1"
Start in:  C:\Users\Administrator\Documents\GitHub\wams\backend
```

### Task 2: WAMS Frontend

Trigger:

```text
At startup, delay 60 seconds
```

Action:

```text
Program:   C:\Program Files\nodejs\npm.cmd
Arguments: start
Start in:  C:\Users\Administrator\Documents\GitHub\wams\frontend
```

Pada kedua task, aktifkan **Allow task to be run on demand** dan pengaturan
restart task jika gagal. Setelah task dibuat, klik kanan task → **Run** untuk
menguji.

Pemeriksaan:

```text
Backend:  http://localhost:8080/health
Frontend: http://localhost:5173
```

Setelah update backend, pilih **End** lalu **Run** pada task `WAMS Backend`.
Setelah update frontend, pilih **End** pada task `WAMS Frontend`, jalankan
`npm run build` di folder frontend, lalu pilih **Run** pada task `WAMS Frontend`.
