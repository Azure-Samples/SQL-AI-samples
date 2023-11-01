from promptflow import tool

# The inputs section will change based on the arguments of the tool function, after you save the code
# Adding type to arguments and return value will help the system show the types properly
# Please update the function name/signature per need
@tool
def my_python_tool(input1: list, input2: list, input3: list, input4: list) -> str:
  retrieved_documents = input1 + input2 + input3 + input4
  return retrieved_documents