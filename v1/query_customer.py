import argparse
import sys
import yaml

import customer1

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-c", "--customer", help="specify the customer item")
   parser.add_argument("-i", "--itemQuery", type=str, choices=["Transport", "Scenario", "Protocol", "Connection", "ConcurrentConnection", "MessageSize", "Send", "Duration", "Interval", "JobConfig"], help="query the item's value")
   args = parser.parse_args()

   callfunc="{customer}.{func}('{item}')".format(customer=args.customer, func="get_info", item=args.itemQuery)
   r = eval(callfunc)
   print (r)
