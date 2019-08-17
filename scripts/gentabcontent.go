package main

import (
	"flag"
	"fmt"
	"os"
	"text/template"
)

type BenchEnv struct {
	TabID		string
	TabHeadline	string
}

func main() {
	env := BenchEnv{os.Getenv("TabID"),
		os.Getenv("TabHeadline")}
	var content = flag.String("content", "", "Specify the content template file")
	var tabcontent = flag.String("tabcontentlist", "", "Specify the tabcontent template file")
	flag.Usage = func() {
		fmt.Println("-content <content tmpl> : specify the content tmpl")
		fmt.Println("-tabcontentlist <tabcontent tmpl> : specify the tabcontent tmpl")
	}
	flag.Parse()
	if content == nil || *content == "" {
		fmt.Println("No content tmpl")
		flag.Usage()
		return
	}
	if tabcontent == nil || *tabcontent == "" {
		fmt.Println("No tabcontent tmpl")
		flag.Usage()
		return
	}
	s1, _ := template.ParseFiles(*content, *tabcontent)
	s1.ExecuteTemplate(os.Stdout, "content", env)
	fmt.Println()
	s1.Execute(os.Stdout, nil)
}
