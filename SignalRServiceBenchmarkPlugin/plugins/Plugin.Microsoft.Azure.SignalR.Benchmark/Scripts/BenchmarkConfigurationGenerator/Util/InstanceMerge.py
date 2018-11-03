def mergeObjectProperties(objectToMergeFrom, objectToMergeTo):
    """
    Used to copy properties from one object to another if there isn't a naming conflict;
    """
    for property in objectToMergeFrom.__dict__:
        #Check to make sure it can't be called... ie a method.
        #Also make sure the objectobjectToMergeTo doesn't have a property of the same name.
        if not callable(objectToMergeFrom.__dict__[property]) and not hasattr(objectToMergeTo, property):
            setattr(objectToMergeTo, property, getattr(objectToMergeFrom, property))

    return objectToMergeTo