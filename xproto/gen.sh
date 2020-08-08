#/bin/sh
export PATH=$PATH:$HOME/go/bin
protoc --proto_path=./ --go_out=./ *.proto

