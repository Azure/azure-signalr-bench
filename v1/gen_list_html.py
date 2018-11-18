import argparse
import jinja2
import os
import sys

def render(tmplPath, scriptContent, divContent):
    context = {
      "JS_LIST":scriptContent,
      "TABLE_DIV_LIST":divContent
    }
    path, filename = os.path.split(tmplPath)
    return jinja2.Environment(
          loader = jinja2.FileSystemLoader(path or './')
       ).get_template(filename).render(context)
    
def generate_html(iDir, tmplFile):
    scriptList = ""
    tabDivList = ""
    for root, dirs, files in os.walk(iDir):
        for file in files:
            if file.endswith("health_stat.js"):
               a = os.path.join(root, file)
               fname = os.path.basename(os.path.splitext(a)[0])
               sList = """
   <script type="text/javascript" src="{s}"></script>""".format(s=a)
               tList = """
                    <div id="{d}_table_div"></div>""".format(d=fname)
               if len(scriptList) == 0:
                  scriptList=sList
               else:
                  scriptList=scriptList+sList
               if len(tabDivList) == 0:
                  tabDivList = tList
               else:
                  tabDivList=tabDivList+tList
    #print(scriptList)
    #print(tabDivList)
    print(render(tmplFile, scriptList, tabDivList))
if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--inputDir", help="Specify the folder whose subdirectories contain *health_stat.js")
   parser.add_argument("-t", "--templateFile", help="Specify the template file path")
   args = parser.parse_args()
   generate_html(args.inputDir, args.templateFile)
