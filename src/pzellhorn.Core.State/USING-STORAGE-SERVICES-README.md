**Steps to enabling Storage in host project**

--GCP Cloud Storage (blob storage)-- 
1. Copy service account secret key to project and place "GCS_BUCKET" and "GOOGLE_APPLICATION_CREDENTIALS" values into .env
2. Ensure service account has access to target bucket.
3. Add StorageExtensions to host project, and make sure GCP storage is the implemented IStorageManager

--Disk Storage--
1. Register the Disk storage in StorageExtensions with the base path to read/write to/from