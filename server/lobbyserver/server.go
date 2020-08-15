package gameserver

import (
	"net/http"
	"time"

	"github.com/julienschmidt/httprouter"
	"github.com/rs/cors"
	log "github.com/sirupsen/logrus"
)

var (
	rootRouter = httprouter.New()
)

const (
	rootPath = "/game/:uuid"
)

// Params parameters
type Params struct {
	// server listen address
	ListenAddr string

	AllowCrossOrigin bool
}

// registerHTTPHandlers add request handlers to router
func registerHTTPHandlers() {
	// websocketPath := rootPath + "/ws/:playertype"
	// rootRouter.Handle("GET", websocketPath, acceptWebsocket)
}

// StartHTTPServer start http/websocket server
func StartHTTPServer(params *Params) {

	registerHTTPHandlers()

	var hh http.Handler
	if params.AllowCrossOrigin {
		// for cross-orgin access,e.g. access from browser when debugging
		c := cors.New(cors.Options{
			AllowOriginFunc: func(origin string) bool {
				return true
			},
			AllowCredentials: true,
			AllowedHeaders:   []string{"*"},           // we need this line for cors to allow cross-origin
			ExposedHeaders:   []string{"Set-Session"}, // we need this line for cors to set Access-Control-Expose-Headers
		})
		hh = c.Handler(rootRouter)
	} else {
		// not allow cross orgin access
		hh = rootRouter
	}

	s := &http.Server{
		Addr:           params.ListenAddr,
		Handler:        hh,
		ReadTimeout:    5 * time.Second,
		WriteTimeout:   5 * time.Second,
		MaxHeaderBytes: 1 << 10,
	}

	log.Printf("Http server listen at:%s", params.ListenAddr)

	err := s.ListenAndServe()
	if err != nil {
		log.Fatalf("Http server ListenAndServe %s failed:%s", params.ListenAddr, err)
	}
}
