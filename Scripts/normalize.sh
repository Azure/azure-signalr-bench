if [ $# -ne 2 ]
then
	echo "Specify the <input_file> <output_file>"
	exit 1
fi
input=$1
output=$2
echo "[" > $output
line=`wc -l $input |awk '{printf("%d", $1-1)}'`
head -n $line $input >> $output
tail -n 1 $input|awk -F }, '{print $1"}"}' >> $output
echo "]" >> $output
