package main

import (
	"flag"
	"fmt"
	"os"

	log "github.com/sirupsen/logrus"

	"shisanshui/config"
	"shisanshui/gameserver"
	"shisanshui/wait"
)

var (
	filepath = ""
	daemon   = ""

	version     string
	longVersion string
)

func init() {
	flag.StringVar(&filepath, "c", "", "specify the config file")
	flag.StringVar(&daemon, "d", "yes", "specify daemon mode")
}

// getVersion get version
func getVersion() string {
	return version
}

func getLongVersion() string {
	return longVersion
}

func main() {
	version := flag.Bool("v", false, "show version")
	lversion := flag.Bool("lv", false, "show long version")
	flag.Parse()

	if *version {
		fmt.Printf("%s\n", getVersion())
		os.Exit(0)
	}

	if *lversion {
		fmt.Printf("%s\n", getLongVersion())
		os.Exit(0)
	}

	if filepath == "" {
		log.Println("please specify config yaml file path use -c")
		os.Exit(1)
	}

	err := config.Init(filepath)
	if err != nil {
		log.Panicln("config faield:", err)
	}

	log.Println("try to start  shisanshui server, version:", getVersion())
	params := &gameserver.Params{
		ListenAddr: fmt.Sprintf(":%d", config.ServerPort),
	}

	// start http server
	go gameserver.StartHTTPServer(params)
	log.Println("start shisanshui server ok!")

	if daemon == "yes" {
		wait.GetSignal()
	} else {
		wait.GetInput()
	}
	return
}
