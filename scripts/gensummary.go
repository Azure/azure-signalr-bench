package main

import (
	"flag"
	"fmt"
	"os"
	"text/template"
)

func main() {
	var header = flag.String("header", "", "Specify the header template file")
	var content = flag.String("content", "", "Specify the content template file")
	var body = flag.String("body", "", "Specify the body template file")
	var footer = flag.String("footer", "", "Specify the footer template file")
	flag.Usage = func() {
		fmt.Println("-header <header tmpl>   : specify the header tmpl")
		fmt.Println("-content <content tmpl> : specify the content tmpl")
		fmt.Println("-body <body tmpl>       : specify the body tmpl")
		fmt.Println("-footer <footer tmpl>   : specify the footer tmpl")
	}
	flag.Parse()
	if header == nil || *header == "" {
		fmt.Println("No header tmpl")
		flag.Usage()
		return
	}
	if content == nil || *content == "" {
		fmt.Println("No content tmpl")
		flag.Usage()
		return
	}
	if footer == nil || *footer == "" {
		fmt.Println("No footer tmpl")
		flag.Usage()
		return
	}
	if body == nil || *body == "" {
		fmt.Println("No body tmpl")
		flag.Usage()
		return
	}
	s1, _ := template.ParseFiles(*header, *content, *body, *footer)
	s1.ExecuteTemplate(os.Stdout, "content", nil)
	fmt.Println()
	s1.Execute(os.Stdout, nil)
}
