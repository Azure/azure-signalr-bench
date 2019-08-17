# encoding=utf8
import sys
reload(sys)
sys.setdefaultencoding('utf8')

import argparse
import os
import jinja2

def render(tpl_path, context):
    path, filename = os.path.split(tpl_path)
    return jinja2.Environment(
            loader = jinja2.FileSystemLoader(path or './')
            ).get_template(filename).render(context)

def load_render(tpl_path):
    UsersPerSecond = os.getenv("UsersPerSecond", 100)
    Duration = os.getenv("Duration", 50)
    Endpoint = os.getenv("Endpoint", None)
    Hub = os.getenv("Hub", "chat")
    Key = os.getenv("Key", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
    Benchmark = os.getenv("Benchmark", "echo_selfhost_json")
    ServiceEndpoint = os.getenv("ServiceEndpoint", "localhost:5001")
    BroadcastThreshold = os.getenv("BroadcastThreshold", 5000)
    BenchEndpoint = os.getenv("BenchEndpoint")
    SignalRServiceExtSSHEndpoint = os.getenv("SignalRServiceExtSSHEndpoint")
    SignalRServiceIntEndpoint = os.getenv("SignalRServiceIntEndpoint")
    SignalRDemoAppExtSSHEndpoint = os.getenv("SignalRDemoAppExtSSHEndpoint")
    SignalRDemoAppIntEndpoint = os.getenv("SignalRDemoAppIntEndpoint")
    context = {
        "UsersPerSecond":UsersPerSecond,
        "Duration":Duration,
        "Endpoint":Endpoint,
        "Hub":Hub,
        "Key":Key,
        "Benchmark":Benchmark,
        "ServiceEndpoint":ServiceEndpoint,
        "BroadcastThreshold":BroadcastThreshold,
        "BenchEndpoint":BenchEndpoint,
        "SignalRServiceExtSSHEndpoint":SignalRServiceExtSSHEndpoint,
        "SignalRServiceIntEndpoint":SignalRServiceIntEndpoint,
        "SignalRDemoAppExtSSHEndpoint":SignalRDemoAppExtSSHEndpoint,
        "SignalRDemoAppIntEndpoint":SignalRDemoAppIntEndpoint
    }
    print render(tpl_path, context)


if __name__=="__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('-t', action='store', dest='tmpl',
                        help='Specify the template file path')
    results = parser.parse_args()
    if results.tmpl != None:
        load_render(results.tmpl)
    else:
        print "Please specify template path"
