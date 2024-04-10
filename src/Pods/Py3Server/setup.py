from setuptools import setup, find_packages

setup(
    name="perf_wps_server",
    version="0.1.0",
    packages=find_packages(),
    install_requires=[
        'azure-messaging-webpubsubclient',
    ],
    classifiers=[
        'Programming Language :: Python :: 3',
        'License :: OSI Approved :: MIT License',
        'Operating System :: OS Independent',
    ],
    python_requires='>=3.6',
)
