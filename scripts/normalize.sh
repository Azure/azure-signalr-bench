if [ $# -ne 2 ]
then
	echo "Specify the <input_file> <output_file>"
	exit 1
fi
input=$1
output=$2
python normalize_counters.py -i $input > $output
