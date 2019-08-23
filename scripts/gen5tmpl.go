package main

import (
	"flag"
	"fmt"
	"os"
	"text/template"
)

func main() {
	var content = flag.String("content", "", "Specify the content template file")
	var t1 = flag.String("t1", "", "Specify required tmpl1")
	var t2 = flag.String("t2", "", "Specify required tmpl2")
	var t3 = flag.String("t3", "", "Specify required tmpl3")
	var t4 = flag.String("t4", "", "Specify required tmpl4")
	flag.Usage = func() {
		fmt.Println("-t1 <tmpl>   : specify the header tmpl")
		fmt.Println("-t2 <tmpl>   : specify the header tmpl")
		fmt.Println("-t3 <tmpl>   : specify the header tmpl")
		fmt.Println("-t4 <tmpl>   : specify the header tmpl")
		fmt.Println("-content <content tmpl> : specify the content tmpl")
	}
	flag.Parse()
	if t1 == nil || *t1 == "" {
		fmt.Println("No t1 tmpl")
		flag.Usage()
		return
	}
	if t2 == nil || *t2 == "" {
		fmt.Println("No t2 tmpl")
		flag.Usage()
		return
	}
	if t3 == nil || *t3 == "" {
		fmt.Println("No t3 tmpl")
		flag.Usage()
		return
	}
	if t4 == nil || *t4 == "" {
		fmt.Println("No t4 tmpl")
		flag.Usage()
		return
	}
	if content == nil || *content == "" {
		fmt.Println("No content tmpl")
		flag.Usage()
		return
	}
	s1, _ := template.ParseFiles(*content, *t1, *t2, *t3, *t4)
	s1.ExecuteTemplate(os.Stdout, "content", nil)
	fmt.Println()
	s1.Execute(os.Stdout, nil)
}
