import os
import re

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    n = len(content)
    output = []
    i = 0
    
    # States
    NORMAL = 0
    STRING = 1
    CHAR = 2
    LINE_COMMENT = 3
    BLOCK_COMMENT = 4
    
    state = NORMAL
    
    while i < n:
        # Check transitions
        if state == NORMAL:
            if content[i:].startswith('//'):
                state = LINE_COMMENT
                output.append(content[i])
                output.append(content[i+1])
                i += 2
                continue
            elif content[i:].startswith('/*'):
                state = BLOCK_COMMENT
                output.append(content[i])
                output.append(content[i+1])
                i += 2
                continue
            elif content[i] == '"':
                state = STRING
                output.append(content[i])
                i += 1
                continue
            elif content[i] == "'":
                state = CHAR
                output.append(content[i])
                i += 1
                continue
            else:
                # Check for Debug.Log / Debug.LogWarning
                # Ensure word boundary
                match = None
                # Check preceding char for word boundary
                is_boundary =  (i == 0 or not (content[i-1].isalnum() or content[i-1] == '_' or content[i-1] == '.'))
                
                if is_boundary:
                    if content[i:].startswith("Debug.Log") or content[i:].startswith("UnityEngine.Debug.Log"):
                        # Check it's not LogError
                        # Potential starts: Debug.Log( or Debug.LogWarning(
                        # We need to distinguish Debug.Log vs Debug.LogWarning vs Debug.LogError
                        
                        # Find the opening parenthesis to extract the method name
                        # Scan ahead slightly to confirm
                        temp_i = i
                        while temp_i < n and content[temp_i] not in ['(', ';', '\n']:
                            temp_i += 1
                        
                        segment = content[i:temp_i]
                        # Remove whitespace for check
                        seg_clean = segment.replace(" ", "")
                        
                        # Valid targets: "Debug.Log", "Debug.LogWarning", "UnityEngine.Debug.Log", "UnityEngine.Debug.LogWarning"
                        # Invalid: "Debug.LogError", "Debug.LogException", "Debug.LogAssertion"
                        
                        # Basic logic: 
                        # Must contain "Debug.Log"
                        # Must NOT contain "Error", "Exception", "Assertion"
                        
                        if ("Debug.Log" in seg_clean and 
                            "Error" not in seg_clean and 
                            "Exception" not in seg_clean and 
                            "Assertion" not in seg_clean and 
                            "(" in content[temp_i:]): # Must have a paren eventually
                            
                            # START OF LOG STATEMENT
                            # We need to find the full extent until semicolon
                            start_idx = i
                            end_idx = find_statement_end(content, i)
                            
                            if end_idx != -1:
                                log_statement = content[start_idx:end_idx]
                                # Safe comment out: /* ... */
                                # Handle existing */ in the string
                                safe_log = log_statement.replace("*/", "* /")
                                output.append("/* " + safe_log + " */")
                                i = end_idx
                                continue
        
        elif state == LINE_COMMENT:
            if content[i] == '\n':
                state = NORMAL
        elif state == BLOCK_COMMENT:
            if content[i:].startswith('*/'):
                state = NORMAL
                output.append(content[i])
                output.append(content[i+1])
                i += 2
                continue
        elif state == STRING:
            if content[i] == '\\':
                output.append(content[i])
                i += 1 # Skip escaped char
                if i < n:
                    output.append(content[i])
                    i += 1
                continue
            elif content[i] == '"':
                state = NORMAL
        elif state == CHAR:
            if content[i] == '\\':
                output.append(content[i])
                i += 1
                if i < n:
                    output.append(content[i])
                    i += 1
                continue
            elif content[i] == "'":
                state = NORMAL
        
        if i < n:
            output.append(content[i])
            i += 1

    return "".join(output)

def find_statement_end(content, start_index):
    n = len(content)
    i = start_index
    
    # Skip until '('
    while i < n and content[i] != '(':
        i += 1
    
    if i >= n: return -1
    
    paren_depth = 0
    in_string = False
    in_char = False
    
    while i < n:
        c = content[i]
        
        if not in_string and not in_char:
            if c == '"':
                in_string = True
            elif c == "'":
                in_char = True
            elif c == '(':
                paren_depth += 1
            elif c == ')':
                paren_depth -= 1
            elif c == ';' and paren_depth == 0:
                return i + 1 # Include semicolon
        elif in_string:
            if c == '\\':
                i += 1
            elif c == '"':
                in_string = False
        elif in_char:
            if c == '\\':
                i += 1
            elif c == "'":
                in_char = False
                
        i += 1
    return -1

def main():
    target_dir = r"d:\Github\Gemini_CLI_test\Spyke_Case\Assets\Scripts"
    print(f"Scanning directory: {target_dir}")
    
    for root, dirs, files in os.walk(target_dir):
        for file in files:
            if file.endswith(".cs"):
                filepath = os.path.join(root, file)
                try:
                    new_content = process_file(filepath)
                    
                    with open(filepath, 'r', encoding='utf-8') as f:
                        old_content = f.read()
                        
                    if new_content != old_content:
                        with open(filepath, 'w', encoding='utf-8') as f:
                            f.write(new_content)
                        print(f"Updated: {filepath}")
                except Exception as e:
                    print(f"Error processing {filepath}: {e}")

if __name__ == "__main__":
    main()
