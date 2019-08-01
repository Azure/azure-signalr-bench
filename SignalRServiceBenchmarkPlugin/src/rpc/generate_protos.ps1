
$USERPROFILE = (get-item env:UserProfile).Value

$PROTOC = $USERPROFILE + "\.nuget\packages\Google.Protobuf.Tools\3.6.1\tools\windows_x64\protoc.exe"
$PLUGIN = $USERPROFILE + "\.nuget\packages\Grpc.Tools\1.15.0\tools\windows_x64\grpc_csharp_plugin.exe"

& $PROTOC -Iprotos --csharp_out build  protos/rpc.proto --grpc_out build --plugin=protoc-gen-grpc=$PLUGIN