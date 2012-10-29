google git: WK2hx2Et2Pc4


Conditions to check for extract method:
	a.  The statement list does not define local variables that are used outside of them, otherwise returning these local variables if only one. 
	(Handled by data flow analysis)
	b.  The statement list should not include branches that lead to other part not in the list. (Handled by control flow analysis)
	c. The statements in the same method but not in the extracted list should not have a branch into the middle of the extracted statements. (handled by control flow analysis)
	d.  Methods and fields that are visible to the original method shall be also visible to the extracted method. (handled by static analysis, if in the same class)
	e. Local variable accessed in the extracted statements but are not fields shall be passed as parameters to the new method. (handled by data flow analysis) 
	f. semantically equivalency between the statement to be extract and the statement in the newly created method. (string comparison, tolerate to non-refactoring changes, AST comparison is tolerable to rename)
	g. method name does not exist. (static analysis)
	h. method does not hide a method in super class. (static analysis)

Counting line of code:  
	Select Edit -> Find & Replace -> Find in files¡­ or just press CTRL+SHIFT+F
	Check Use and select Regular expressions.
	Top Left Drop down using the Find in Files Selection
	Type the following as the text to find:
	For C#: ^~(:Wh@//.+)~(:Wh@\{:Wh@)~(:Wh@\}:Wh@)~(:Wh@/#).+