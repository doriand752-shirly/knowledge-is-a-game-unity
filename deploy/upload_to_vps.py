"""Upload Knowledge is a Game (Unity/Flutter stack) Colyseus runtime to VPS /opt/quizene."""
from __future__ import annotations

import sys
import tarfile
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, r"c:\Users\doria\palworld")
from _vps import connect, run  # noqa: E402

REMOTE = "/opt/quizene"
PUBLIC_WEB_URL = "http://85.17.145.134:2567"
REMOTE_TAR = "/tmp/quizene-runtime.tgz"

INCLUDE = [
    "package.json",
    "package-lock.json",
    "tsconfig.base.json",
    ".dockerignore",
    "content",
    "deploy/Dockerfile.runtime",
    "deploy/docker-compose.yml",
    "packages/shared/package.json",
    "packages/shared/dist",
    "apps/server/package.json",
    "apps/server/dist",
    "apps/web/package.json",
    "apps/web/dist",
]


def make_tarball(out: Path) -> None:
    with tarfile.open(out, "w:gz") as tar:
        for rel in INCLUDE:
            path = ROOT / rel
            if not path.exists():
                raise SystemExit(f"missing: {path}")
            tar.add(path, arcname=rel.replace("\\", "/"))


def main() -> int:
    with tempfile.TemporaryDirectory() as td:
        local_tar = Path(td) / "quizene-runtime.tgz"
        print("packing…")
        make_tarball(local_tar)
        print(f"archive {local_tar.stat().st_size // 1024} KB")

        c = connect()
        sftp = c.open_sftp()
        print(f"uploading → {REMOTE_TAR}")
        sftp.put(str(local_tar), REMOTE_TAR)
        sftp.close()
        c.close()

    cmd = f"""
set -e
mkdir -p {REMOTE}
rm -rf {REMOTE}/apps {REMOTE}/packages {REMOTE}/content
tar -xzf {REMOTE_TAR} -C {REMOTE}
cd {REMOTE}/deploy
PUBLIC_WEB_URL={PUBLIC_WEB_URL} docker compose up -d --build
docker ps --filter name=quizene
sleep 2
curl -sf http://127.0.0.1:2567/health
echo
curl -sf -o /dev/null -w '%{{http_code}}\\n' http://127.0.0.1:2567/
echo deploy_ok
""".strip()
    return run(cmd)


if __name__ == "__main__":
    raise SystemExit(main())
