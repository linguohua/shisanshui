package main

import (
	"flag"
	"fmt"
	"os"

	log "github.com/sirupsen/logrus"

	"shisanshui/gameserver"
	"shisanshui/wait"
)

var (
	listenAddr = ""
	daemon     = ""

	version     string
	longVersion string
)

func init() {
	flag.StringVar(&listenAddr, "l", ":4443", "specify the listen address")
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

	log.Println("try to start  shisanshui server, version:", getVersion())

	params := &gameserver.Params{
		ListenAddr: listenAddr,
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
