package main

import (
	"flag"
	"fmt"
	"os"
	"text/template"
)

type BenchEnv struct {
	OnlineConnections    string
	ActiveConnections    string
	ConcurrentConnection string
	Duration             string
	Endpoint             string
	Hub                  string
	Key                  string
	Benchmark            string
}

func main() {
	env := BenchEnv{os.Getenv("OnlineConnections"),
		os.Getenv("ActiveConnections"),
		os.Getenv("ConcurrentConnection"),
		os.Getenv("Duration"),
		os.Getenv("Endpoint"),
		os.Getenv("Hub"),
		os.Getenv("Key"),
		os.Getenv("Benchmark")}
	var header = flag.String("header", "", "Specify the header template file")
	var content = flag.String("content", "", "Specify the content template file")
	var footer = flag.String("footer", "", "Specify the footer template file")
	flag.Usage = func() {
		fmt.Println("-header <header tmpl>   : specify the header tmpl")
		fmt.Println("-content <content tmpl> : specify the content tmpl")
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
	s1, _ := template.ParseFiles(*header, *content, *footer)
	s1.ExecuteTemplate(os.Stdout, "content", env)
	fmt.Println()
	s1.Execute(os.Stdout, nil)
}
