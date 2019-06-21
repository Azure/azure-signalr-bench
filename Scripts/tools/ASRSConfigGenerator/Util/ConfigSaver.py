import yaml


def save_yaml(config, path):
    with open(path, 'w') as f:
        f.write(yaml.dump(config, Dumper=yaml.Dumper, default_flow_style=False))
