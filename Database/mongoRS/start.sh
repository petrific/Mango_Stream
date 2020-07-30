set -x
MONGO_LOG="/var/log/mongodb/mongod.log"
MONGO="mongo"
MONGOD="mongod"
$MONGOD  --fork --bind_ip_all --replSet rs0 --noauth --logpath $MONGO_LOG

checkSecondaryStatus(){
CURRENT=$1
$MONGO --host $CURRENT --eval db
while [ "$?" -ne 0 ]
do
sleep 10
$MONGO --host $CURRENT --eval db
done
}

if [ "$ROLE" == "main" ]
then
checkSecondaryStatus $SECONDARY1
checkSecondaryStatus $SECONDARY2
$MONGO --eval "rs.initiate({
  _id : 'rs0',
  members: [
    { _id : 0, host : \"$MAIN:27017\" },
    { _id : 1, host : \"$SECONDARY1:27017\" },
    { _id : 2, host : \"$SECONDARY2:27017\" }
  ]
})"
fi
tail -f /dev/null
