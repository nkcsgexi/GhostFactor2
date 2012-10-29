using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using warnings.util;

namespace warnings.analyzer
{
    /* This is builder class of a call graph.*/ 
    public class CallGraphBuilder
    {
        /* The class where a call graph should ananlyze. */
        private readonly ClassDeclarationSyntax classDeclaration;

        /* The entire syntax tree of the file containing this class defined above. */
        private readonly SyntaxTree tree;

        public CallGraphBuilder(ClassDeclarationSyntax classDeclaration, 
            SyntaxTree tree)
        {
            this.classDeclaration = classDeclaration;
            this.tree = tree;
        }

        /* Build the graph. */
        public CallGraph BuildCallGraph()
        {
            // Get all the method declarations in the class.
            var methods = classDeclaration.DescendantNodes().Where(n => n.Kind == SyntaxKind.MethodDeclaration);
            var callGraph = new CallGraph();
            
            // Add all the methods to the call graph as vertices.
            foreach (MethodDeclarationSyntax m in methods)
            {
                callGraph.addVertice(m);
            }

            // For each pair of method, check if they have caller/callee relationship and adds to 
            // the call graph as edges if true.
            foreach (var m1 in methods)
            {
                foreach (var m2 in methods)
                {
                    if (ASTUtil.IsInvoking(m1, m2, tree))
                    {
                        callGraph.addEdge((MethodDeclarationSyntax)m1, (MethodDeclarationSyntax)m2);
                    }
                }
            }
            return callGraph;
        }

    }

    /* This represents a call graph, with lists of verices and edges. */
    public class CallGraph
    {
        private readonly IList<MethodDeclarationSyntax> vertices = new List<MethodDeclarationSyntax>();
        private readonly IList<KeyValuePair<MethodDeclarationSyntax, MethodDeclarationSyntax>> edges 
            = new List<KeyValuePair<MethodDeclarationSyntax, MethodDeclarationSyntax>>();
        
        public void addVertice(MethodDeclarationSyntax method)
        {
            vertices.Add(method);
        }

        public IList<MethodDeclarationSyntax> getVertices()
        {
            return vertices;
        }

        /* Get the method declaration with the givien name. */
        public MethodDeclarationSyntax getVerticeByIdentifier(String identifier)
        {
            var method = getMethodWithIdentifier(vertices, identifier);
            if(method == null)
                throw new Exception("cannot find method with specified identifier");
            else
                return method;
        }

        public IList<KeyValuePair<MethodDeclarationSyntax, MethodDeclarationSyntax>> getEdges()
        {
            return edges;
        }

        public void addEdge(MethodDeclarationSyntax caller, MethodDeclarationSyntax callee)
        {
            edges.Add(new KeyValuePair<MethodDeclarationSyntax, MethodDeclarationSyntax>(caller, callee));
        }

        /* Whether the graph has a caller and callee edge that is as given. */
        public bool hasEdge(MethodDeclarationSyntax caller, MethodDeclarationSyntax callee)
        {
            foreach (var pair in edges)
            {
                if(hasSameIdentifier(pair.Key, caller) && hasSameIdentifier(pair.Value, callee))
                    return true;
            }
            return false;
        }

        public bool hasEdge(String callerId, String calleeId)
        {
            foreach (var pair in edges)
            {
                if (pair.Key.Identifier.Value.Equals(callerId) && pair.Value.Identifier.Value.Equals(calleeId))
                    return true;
            }
            return false;
        }

        /* Whether has the method declaration having the same name. */
        public bool hasVertice(MethodDeclarationSyntax method)
        {
            return getMethodWithIdentifier(vertices, (String)method.Identifier.Value) != null;
        }

       
        public bool hasVertice(String identifier)
        {
            return getMethodWithIdentifier(vertices, identifier) != null;
        }

        /* Get the method with the given ID. */
        public MethodDeclarationSyntax getVertice(String identifier)
        {
            var method = getMethodWithIdentifier(vertices, identifier);
            if(method == null)
                throw new Exception("No vertice with specified id is found.");
            else return method;
        }

        /* Get the list of method declarations that are in this call graph but not in another. */
        public IList<MethodDeclarationSyntax> GetVerticesNotIn(CallGraph another)
        {
            IList<MethodDeclarationSyntax> methods = new List<MethodDeclarationSyntax>();
            foreach(var m in vertices)
            {
                if(!another.hasVertice(m))
                    methods.Add(m);
            }
            return methods;
        }

        /* Get the common vertices(methods) between this call graph and another. */
        public IList<MethodDeclarationSyntax> GetCommonVertices(CallGraph another)
        {
            IList<MethodDeclarationSyntax> methods = new List<MethodDeclarationSyntax>();
            foreach (var m in vertices)
            {
                if(another.hasVertice(m))
                    methods.Add(m);
            }
            return methods;
        }

        /* Common utility to decide whether a list of methods has a method with the given identifier. */
        private static MethodDeclarationSyntax getMethodWithIdentifier(IList<MethodDeclarationSyntax> methods, String identifier)
        {
            foreach (var method in methods)
            {
                if (method.Identifier.Value.Equals(identifier))
                    return method;
            }
            return null;
        }

        /* Check if two methods share the same name. */
        private static bool hasSameIdentifier(MethodDeclarationSyntax m1, MethodDeclarationSyntax m2)
        {
            return m1.Identifier.Value.Equals(m2.Identifier.Value);
        }
    }

}
