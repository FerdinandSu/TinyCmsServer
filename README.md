# TinyCmsServer

A tiny file server that serves static files from harddrive and allows user to read/write/overwrite/delete files.

## Endpoints

- /Files/[path]: Get file or directory
- /Exists/[path]: Check if file exists
- /Delete/[path]: Delete file
- /DeleteDir/[path]: Delete directory
- /Upload/[path]: Upload file
- /UploadOrUpdate/[path]: Upload or Overwrite file
- /swagger: Swagger UI

Notice: **NO Access Control is provided by TinyCmsServer**.

## Environment Variables

- TINY_CMS_DIR: Path to the root directory of contents served by TinyCmsServer
- TINY_CMS_URL: Kestrel Endpoint URL of TinyCmsServer, e.g.
  - `http://localhost:80` for local usage
  - `http://0.0.0.0:80` for public usage
