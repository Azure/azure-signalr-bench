#/bin/bash

python3 -m venv venv
source venv/bin/activate

pip install pip-tools
pip-compile pyproject.toml
