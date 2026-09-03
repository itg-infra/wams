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

## Menjalankan Backend

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
