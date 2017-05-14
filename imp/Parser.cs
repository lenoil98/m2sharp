﻿/* M2Sharp -- Modula-2 to C# Translator & Compiler
 *
 * Copyright (c) 2016 The Modula-2 Software Foundation
 *
 * Author & Maintainer: Benjamin Kowarsch <trijezdci@org.m2sf>
 *
 * @synopsis
 *
 * M2Sharp is a multi-dialect Modula-2 to C# translator and via-C# compiler.
 * It supports the dialects described in the 3rd and 4th editions of Niklaus
 * Wirth's book "Programming in Modula-2" (PIM) published by Springer Verlag,
 * and an extended mode with select features from the revised language by
 * B.Kowarsch and R.Sutcliffe "Modula-2 Revision 2010" (M2R10).
 *
 * In translator mode, M2Sharp translates Modula-2 source to C# source files.
 * In compiler mode, M2Sharp compiles Modula-2 source via C# source files
 * to object code or executables using the host system's C# compiler.
 *
 * @repository
 *
 * https://github.com/m2sf/m2sharp
 *
 * @file
 *
 * Parser.cs
 *
 * Parser class.
 *
 * @license
 *
 * M2Sharp is free software: you can redistribute and/or modify it under the
 * terms of the GNU Lesser General Public License (LGPL) either version 2.1
 * or at your choice version 3 as published by the Free Software Foundation.
 * However, you may not alter the copyright, author and license information.
 *
 * M2Sharp is distributed in the hope that it will be useful,  but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  Read the license for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with M2Sharp. If not, see <https://www.gnu.org/copyleft/lesser.html>.
 *
 * NB: Components in the domain part of email addresses are in reverse order.
 */

namespace org.m2sf.m2sharp {

public class AstNode { /* temporary definition */

public static AstNode NewNode (uint type, AstNode[] args) {
  return null;
} /* end NewNode */

public static AstNode NewListNode (uint type, AstNode[] args) {
  return null;
} /* end NewNode */

public static AstNode NewTerminalNode (uint type, AstNode[] args) {
  return null;
} /* end NewNode */

public static AstNode NewTerminalListNode (uint type, AstNode[] args) {
  return null;
} /* end NewNode */

public void ReplaceSubnode (uint index, AstNode replacement) {

} /* end ReplaceSubnode */

} /* AstNode */


public class Fifo { /* temporary definition */

public static Fifo NewQueue () {
  return null;
} /* end NewQueue */

public void Enqueue (string s) {

} /* end Enqueue */

public string Dequeue () {
  return null;
} /* end Dequeue */

public void ReleaseQueue () {

} /* end Release Queue */

} /* Fifo */


public class Parser {

private Lexer lexer;
private AstNode ast;
private uint warningCount;
private uint errorCount;
private uint status;


/* --------------------------------------------------------------------------
 * private method compilationUnit()
 * --------------------------------------------------------------------------
 * compilationUnit :=
 *   definitionModule | implementationModule | programModule
 *   ;
 *
 * astnode: defModuleNode | impModuleNode
 * ----------------------------------------------------------------------- */

private Token compilationUnit () {
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("compilationUnit");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  switch (lookahead) {
    case Token.DEFINITION :
      lookahead = definitionModule();
      break;
      
    case Token.IMPLEMENTATION :
      lookahead = implementationModule();
      break;
      
    case Token.MODULE :
      lookahead = programModule();
      break;
  } /* end switch */
  
  return lookahead;
} /* end compilationUnit */


/* --------------------------------------------------------------------------
 * private method definition_module()
 * --------------------------------------------------------------------------
 * definitionModule :=
 *   DEFINITION MODULE moduleIdent ';'
 *   import* definition* END moduleIdent '.'
 *   ;
 *
 * moduleIdent := Ident ;
 *
 * astnode: (DEFMOD identNode implist deflist)
 * ----------------------------------------------------------------------- */

private Token definitionModule () {
  AstNode id, implist, deflist;
  string ident1 = null, ident2;
  Fifo tmplist;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("definitionModule");
  } /* end if */
  
  /* DEFINITION */
  lookahead = lexer.ConsumeSym();
  
  /* MODULE */
  if (matchToken(Token.MODULE, RESYNC(IMPORT_OR_DEFINITON_OR_END))) {
    lookahead = lexer.ConsumeSym();
    
    /* moduleIdent */
    if (matchToken(Token.IDENTIFIER, RESYNC(IMPORT_OR_DEFINITON_OR_END))) {
      lookahead = lexer.ConsumeSym();
      ident1 = lexer.CurrentLexeme();
      
      /* ';' */
      if (matchToken(Token.SEMICOLON, RESYNC(IMPORT_OR_DEFINITON_OR_END))) {
        lookahead = lexer.ConsumeSym();
      }
      else /* resync */ {
        lookahead = lexer.NextSym();
      } /* end if */
    }
    else /* resync */ {
      lookahead = lexer.NextSym();
    } /* end if */
  }
  else /* resync */ {
    lookahead = lexer.NextSym();
  } /* end if */
  
  tmplist = Fifo.NewQueue();

  /* import* */
  while ((lookahead == Token.IMPORT) || (lookahead == Token.FROM)) {
    lookahead = import();
    tmplist.Enqueue(ast);
  } /* end while */
  
  implist = AstNode.NewListNode(AST.IMPLIST);
  tmplist.ResetQueue();
  
  /* definition* */
  while ((lookahead == Token.CONST) ||
         (lookahead == Token.TYPE) ||
         (lookahead == Token.VAR) ||
         (lookahead == Token.PROCEDURE)) {
    lookahead = definition();
    tmplist.Enqueue(ast);
  } /* end while */
  
  deflist = AstNode.NewListNode(AST.DEFLIST);
  tmplist.ReleaseQueue();
  
  /* END */
  if (matchToken(Token.END, FOLLOW(DEFINITION_MODULE))) {
    lookahead = lexer.ConsumeSym();
    
    /* moduleIdent */
    if (matchToken(Token.IDENTIFIER, FOLLOW(DEFINITION_MODULE))) {
      lookahead = lexer.ConsumeSym();
      ident2 = lexer.CurrentLexeme();
    
      if (ident1 != ident2) {
        /* TO DO : report error -- module identifiers don't match */ 
      } /* end if */
    
      /* '.' */
      if (matchToken(Token.PERIOD, FOLLOW(DEFINITION_MODULE))) {
        lookahead = lexer.ConsumeSym();
      } /* end if */
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  id = AstNode.NewTerminalNode(AST.Ident, ident1);
  ast = AstNode.NewNode(AST.DEFMOD, id, implist, deflist);
  
  return lookahead;
} /* end definitionModule */


/* --------------------------------------------------------------------------
 * private method import()
 * --------------------------------------------------------------------------
 * import :=
 *   ( qualifiedImport | unqualifiedImport ) ';'
 *   ;
 * ----------------------------------------------------------------------- */


private Token import () { 
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("import");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  /* qualifiedImport */
  if (lookahead == Token.IMPORT) {
    lookahead = qualifiedImport(); /* ast holds ast-node */
  }
  /* | unqualifiedImport */
  else if (lookahead == Token.FROM) {
    lookahead = unqualifiedImport(); /* ast holds ast-node */
  }
  else /* unreachable code */ {
    /* fatal error -- abort */
  } /* end if */
  
  /* ';' */
  if (matchToken(Token.SEMICOLON, RESYNC(IMPORT_OR_DEFINITON_OR_END))) {
    lookahead = lexer.ConsumeSym();
  } /* end if */
  
  /* AST node is passed through in ast */
  
  return lookahead;
} /* end import */


/* --------------------------------------------------------------------------
 * private method qualifiedImport()
 * --------------------------------------------------------------------------
 * qualifiedImport :=
 *   IMPORT moduleList
 *   ;
 *
 * moduleList := identList ;
 *
 * astnode: (IMPORT identListNode)
 * ----------------------------------------------------------------------- */

private Token qualifiedImport () {
  AstNode idlist;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("qualifiedImport");
  } /* end if */
  
  /* IMPORT */
  lookahead = lexer.ConsumeSym();
  
  /* moduleList */
  if (matchToken(Token.IDENTIFIER, FOLLOW(QUALIFIED_IMPORT))) {
    lookahead = identList();
    idlist = ast;
  } /* end if */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.IMPORT, idlist);
  
  return lookahead;
} /* end qualifiedImport */


/* --------------------------------------------------------------------------
 * private method unqualifiedImport()
 * --------------------------------------------------------------------------
 * unqualifiedImport :=
 *   FROM moduleIdent IMPORT identList
 *   ;
 *
 * moduleIdent := Ident ;
 *
 * astnode: (UNQIMP identNode identListNode)
 * ----------------------------------------------------------------------- */

private Token unqualifiedImport () {
  AstNode id, idlist = null;
  string ident = null;
  Token lookahead;

  if (Capabilities.UnqualifiedImport() == false) {
    // unqualified import not supported -- report error and skip
  } /* end if */
    
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("unqualifiedImport");
  } /* end if */
  
  /* FROM */
  lookahead = lexer.ConsumeSym();
  
  /* moduleIdent */
  if (matchToken(Token.IDENTIFIER, RESYNC(IMPORT_OR_IDENT_OR_SEMICOLON))) {
    lookahead = lexer.ConsumeSym();
    ident = lexer.CurrentLexeme();

    /* IMPORT */
    if (matchToken(Token.IMPORT, RESYNC(IDENT_OR_SEMICOLON))) {
      lookahead = lexer.ConsumeSym();
      
      /* moduleList */
      if (matchToken(Token.IDENTIFIER, FOLLOW(UNQUALIFIED_IMPORT))) {
        lookahead = identList();
        idlist = ast;
      } /* end if */
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  id = AstNode.NewTerminalNode(AST.IDENT, ident);
  ast = AstNode.NewNode(AST.UNQIMP, id, idlist);
  
  return lookahead;
} /* end unqualifiedImport */


/* --------------------------------------------------------------------------
 * private method identList()
 * --------------------------------------------------------------------------
 * identList :=
 *   Ident ( ',' Ident )*
 *   ;
 *
 * astnode: (IDENTLIST ident0 ident1 ident2 ... identN)
 * ----------------------------------------------------------------------- */

private Token identList () {
  string ident;
  Fifo tmplist;
  Token lookahead;
  uint line, column;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("identList");
  } /* end if */
  
  /* Ident */
  ident = lexer.LookaheadLexeme();
  lookahead = lexer.ConsumeSym();
  
  /* add ident to temporary list */
  tmplist = Fifo.NewQueue(ident);
  
  /* ( ',' Ident )* */
  while (lookahead == Token.COMMA) {
    /* ',' */
    lookahead = lexer.ConsumeSym();
    
    /* Ident */
    if (matchToken(Token.IDENTIFIER, RESYNC(COMMA_OR_SEMICOLON))) {
      lookahead = lexer.ConsumeSym();
      ident = lexer.CurrentLexeme();
      
      /* check for duplicate identifier */
      if (tmplist.EntryExists(ident)) {
        line = lexer.CurrentLine();
        column = lexer.CurrentColumn();
        // report_error_w_offending_lexeme
        //  (M2C_ERROR_DUPLICATE_IDENT_IN_IDENT_LIST, p,
        //   m2c_lexer_current_lexeme(p->lexer), line, column);
      }
      else /* not a duplicate */ {
        /* add ident to temporary list */
        tmplist.Enqueue(ident);
      } /* end if */
    } /* end if */
  } /* end while */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.IDENTLIST, tmplist);
  tmplist.ReleaseQueue();
    
  return lookahead;
} /* end identList */


/* --------------------------------------------------------------------------
 * private method definition()
 * --------------------------------------------------------------------------
 * definition :=
 *   CONST ( constDefinition ';' )* |
 *   TYPE ( typeDefinition ';' )* |
 *   VAR ( varDefinition ';' )* |
 *   procedureHeader ';'
 *   ;
 *
 * varDefinition := variableDeclaration ;
 * ----------------------------------------------------------------------- */

private Token definition () {
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("definition");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  switch (lookahead) {
    
    /* CONST */
    case Token.CONST :
      lookahead = lexer.ConsumeSym();
      
      /* ( constDefinition ';' )* */
      while (lookahead == Token.IDENTIFIER) {
        lookahead = constDefinition(); /* ast holds ast-node */
        
        /* ';' */
        if (matchToken(Token.SEMICOLON,
            RESYNC(DEFINITION_OR_IDENT_OR_SEMICOLON))) {
          lookahead = lexer.ConsumeSym();
        } /* end if */
      } /* end while */
      break;
      
    /* | TYPE */
    case Token.TYPE :
      lookahead = lexer.ConsumeSym();
      
      /* ( typeDefinition ';' )* */
      while (lookahead == Token.IDENTIFIER) {
        lookahead = typeDefinition(); /* ast holds ast-node */
        
        /* ';' */
        if (matchToken(Token.SEMICOLON,
            RESYNC(DEFINITION_OR_IDENT_OR_SEMICOLON))) {
          lookahead = lexer.ConsumeSym();
        } /* end if */
      } /* end while */
      break;
      
    /* | VAR */
    case TOKEN_VAR :
      lookahead = lexer.ConsumeSym();
      
      /* ( varDefinition ';' )* */
      while (lookahead == Token.IDENTIFIER) {
        lookahead = variableDeclaration(); /* ast holds ast-node */
        
        /* ';' */
        if (match_token(Token.SEMICOLON,
            RESYNC(DEFINITION_OR_IDENT_OR_SEMICOLON))) {
          lookahead = lexer.ConsumeSym();
        } /* end if */
      } /* end while */
      break;
      
    /* | procedureHeader */
    case Token.PROCEDURE :
      lookahead = procedureHeader(); /* ast holds ast-node */
      
      /* ';' */
      if (matchToken(Token.SEMICOLON,
          RESYNC(DEFINITION_OR_SEMICOLON))) {
        lookahead = lexer.ConsumeSym();
      } /* end if */
      break;
      
    default : /* unreachable code */
      /* fatal error -- abort */
      Environment.Exit(-1);
  } /* end switch */
  
  /* AST node is passed through in ast */
  
  return lookahead;
} /* end definition */


/* --------------------------------------------------------------------------
 * private method constDefinition()
 * --------------------------------------------------------------------------
 * constDefinition :=
 *   Ident '=' constExpression
 *   ;
 *
 * astnode: (CONSTDEF identNode exprNode)
 * ----------------------------------------------------------------------- */

private Token constDefinition () {
  AstNode id, expr;
  string ident;
  Token lookahead;
    
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("constDefinition");
  } /* end if */
  
  /* Ident */
  lookahead = lexer.ConsumeSym();
  ident = lexer.CurrentLexeme();
  
  /* '=' */
  if (matchToken(Token.EQUAL, FOLLOW(CONST_DEFINITION))) {
    lookahead = lexer.ConsumeSym();
    
    /* constExpression */
    if (matchSet(FIRST(EXPRESSION), FOLLOW(CONST_DEFINITION))) {
      lookahead = constExpression();
      expr = ast;
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  id = AstNode.NewTerminalNode(AST_IDENT, ident);
  ast = AstNode.NewNode(AST.CONSTDEF, id, expr);
  
  return lookahead;
} /* end constDefinition */


/* --------------------------------------------------------------------------
 * private method type_definition()
 * --------------------------------------------------------------------------
 * typeDefinition :=
 *   Ident ( '=' type )?
 *   ;
 *
 * astnode: (TYPEDEF identNode typeConstructorNode)
 * ----------------------------------------------------------------------- */

private Token typeDefinition () {
  AstNode id, tc;
  string ident;
  Token lookahead;
    
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("typeDefinition");
  } /* end if */
  
  /* Ident */
  lookahead = lexer.ConsumeSym();
  ident = lexer.CurrentLexeme();
  
  /* ( '=' type )? */
  if (lookahead == Token.EQUAL) {
    lookahead = lexer.ConsumeSym();
    
    /* type */
    if (matchSet(FIRST(TYPE), FOLLOW(TYPE_DEFINITION))) {
      lookahead = type();
      tc = ast;
    } /* end if */
  }
  else {
    tc = AstNode.EmptyNode();
  } /* end if */
  
  /* build AST node and pass it back in ast */
  id = AstNode.NewTerminalNode(AST.IDENT, ident);
  ast = AstNode.NewNode(AST.TYPEDEF, id, tc);
  
  return lookahead;
} /* end typeDefinition */


/* --------------------------------------------------------------------------
 * private method type()
 * --------------------------------------------------------------------------
 * type :=
 *   derivedOrSubrangeType | enumType | setType | arrayType |
 *   recordType | pointerType | procedureType
 *   ;
 * ----------------------------------------------------------------------- */

private Token type () {
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("type");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  switch (lookahead) {
  
    /* derivedOrSubrangeType */
    case Token.IDENTIFIER :
    case Token.LEFT_BRACKET :
      lookahead = derivedOrSubrangeType(); /* ast holds ast-node */
      break;
      
    /* | enumType */
    case Token.LEFT_PAREN :
      lookahead = enumType(); /* ast holds ast-node */
      break;
      
    /* | setType */
    case Token.SET :
      lookahead = setType(); /* ast holds ast-node */
      break;
      
    /* | arrayType */
    case Token.ARRAY :
      lookahead = arrayType(); /* ast holds ast-node */
      break;
      
    /* | recordType */
    case Token.RECORD :
      lookahead = recordType(); /* ast holds ast-node */
      break;
      
    /* | pointerType */
    case Token.POINTER :
      lookahead = pointerType(); /* ast holds ast-node */
      break;
      
    /* | procedureType */
    case Token.PROCEDURE :
      lookahead = procedureType(); /* ast holds ast-node */
      break;
      
    default : /* unreachable code */
      /* fatal error -- abort */
      Environment.Exit(-1);
    } /* end switch */
  
  /* AST node is passed through in ast */
  
  return lookahead;
} /* end type */


/* --------------------------------------------------------------------------
 * private method derivedOrSubrangeType()
 * --------------------------------------------------------------------------
 * derivedOrSubrangeType :=
 *   typeIdent range? | range
 *   ;
 *
 * typeIdent := qualident ;
 * ----------------------------------------------------------------------- */

private Token derivedOrSubrangeType () {
  AstNode id;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("derivedOrSubrangeType");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  if (matchSet(FIRST(DERIVED_OR_SUBRANGE_TYPE),
      FOLLOW(DERIVED_OR_SUBRANGE_TYPE))) {
    
    /* typeIdent range? */
    if (lookahead == Token.IDENTIFIER) {
      
      /* typeIdent */
      lookahead = qualident();
      /* astnode: (IDENT ident) | (QUALIDENT q0 q1 q2 ... qN ident) */
      id = ast;
      
      /* range? */
      if (lookahead == Token.LeftBracket) {
        lookahead = range(p);
        ast.ReplaceSubnode(2, id);
        /* astnode: (SUBR lower upper identNode) */
      } /* end if */
    }
    /* | range */
    else if (lookahead == Token.LeftBracket) {
      lookahead = range(p);
      /* astnode: (SUBR lower upper (EMPTY)) */
    }
    else /* unreachable code */ {
      /* fatal error -- abort */
      Environment.Exit(-1);
    } /* end if */
  } /* end if */
  
  /* AST node is passed through in ast */
  
  return lookahead;
} /* end derivedOrSubrangeType */


/* --------------------------------------------------------------------------
 * private method qualident()
 * --------------------------------------------------------------------------
 * qualident :=
 *   Ident ( '.' Ident )*
 *   ;
 *
 * astnode: (IDENT ident) | (QUALIDENT q0 q1 q2 ... qN ident)
 * ----------------------------------------------------------------------- */

private Token qualident () {
  string ident, qident;
  Fifo tmplist;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("qualident");
  } /* end if */
  
  /* Ident */
  lookahead = lexer.ConsumeSym();
  ident = lexer.CurrentLexeme();
  
  /* add ident to temporary list */
  tmplist = Fifo.NewQueue(ident);
  
  /* ( '.' Ident )* */
  while (lookahead == Token.Period) {
    /* '.' */
    lookahead = lexer.ConsumeSym();
    
    /* Ident */
    if (matchToken(Token.IDENTIFIER, FOLLOW(QUALIDENT))) {
      lookahead = lexer.ConsumeSym();
      qident = lexer.CurrentLexeme();
      tmplist.Enqueue(qident);
    } /* end if */
  } /* end while */
  
  /* build AST node and pass it back in ast */
  if (tmplist.EntryCount() == 1) {
    ast = AstNode.NewTerminalNode(AST.IDENT, ident);
  }
  else {
    ast = AstNode.NewTerminalListNode(AST.QUALIDENT, tmplist);
  } /* end if */
  
  tmplist.ReleaseQueue();
  
  return lookahead;
} /* end qualident */


/* --------------------------------------------------------------------------
 * private method range()
 * --------------------------------------------------------------------------
 * range :=
 *   '[' constExpression '..' constExpression ']'
 *   ;
 *
 * astnode: (SUBR exprNode exprNode (EMPTY))
 * ----------------------------------------------------------------------- */

private Token range () {
  AstNode lower, upper, empty;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("range");
  } /* end if */
  
  /* '[' */
  lookahead = lexer.ConsumeSym();
  
  /* constExpression */
  if (matchSet(FIRST(EXPRESSION), FOLLOW(RANGE))) {
    lookahead = constExpression();
    lower = ast;
    
    /* '..' */
    if (matchToken(Token.RANGE, FOLLOW(RANGE))) {
      lookahead = lexer.ConsumeSym();
      
      /* constExpression */
      if (matchSet(FIRST(EXPRESSION), FOLLOW(RANGE))) {
        lookahead = constExpression();
        upper = ast;
        
        /* ']' */
        if (matchToken(Token.RightBracket, FOLLOW(RANGE))) {
          lookahead = lexer.ConsumeSym();
        } /* end if */
      } /* end if */
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  empty = AstNode.EmptyNode();
  ast = AstNode.NewNode(AST.SUBR, lower, upper, empty);
  
  return lookahead;
} /* end range */


/* --------------------------------------------------------------------------
 * private method enumType()
 * --------------------------------------------------------------------------
 * enumType :=
 *   '(' identList ')'
 *   ;
 *
 * astnode: (ENUM identListNode)
 * ----------------------------------------------------------------------- */

private Token enumType () {
  AstNode idlist;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("enumType");
  } /* end if */
  
  /* '(' */
  lookahead = lexer.ConsumeSym();
  
  /* identList */
  if (matchToken(Token.Identifier, FOLLOW(ENUM_TYPE))) {
    lookahead = identList();
    idlist = ast;
    
    /* ')' */
    if (matchToken(Token.RightParen, FOLLOW(ENUM_TYPE))) {
      lookahead = lexer.ConsumeSym();
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.ENUM, idlist);
  
  return lookahead;
} /* end enumType */


/* --------------------------------------------------------------------------
 * private function setType()
 * --------------------------------------------------------------------------
 * setType :=
 *   SET OF countableType
 *   ;
 *
 * astnode: (SET typeConstructorNode)
 * ----------------------------------------------------------------------- */

private Token setType () {
  AstNode tc;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("setType");
  } /* end if */
  
  /* SET */
  lookahead = lexer.ConsumeSym();
  
  /* OF */
  if (matchToken(Token.OF, FOLLOW(SET_TYPE))) {
    lookahead = lexer.ConsumeSym();
    
    /* countableType */
    if (matchToken(Token.Identifier, FOLLOW(SET_TYPE))) {
      lookahead = countableType();
      tc = ast;
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in p->ast */
  ast = AstNode.NewNode(AST.SET, tc);
  
  return lookahead;
} /* end setType */


/* --------------------------------------------------------------------------
 * private method countableType()
 * --------------------------------------------------------------------------
 * countableType :=
 *   range | enumType | countableTypeIdent range?
 *   ;
 *
 * countableTypeIdent := typeIdent ;
 *
 * astnode:
 *  (IDENT ident) | (QUALIDENT q0 q1 q2 ... qN ident) |
 *  (SUBR expr expr (EMPTY)) | (SUBR expr expr identNode) |
 *  (ENUM identListNode)
 * ----------------------------------------------------------------------- */

private Token countableType () {
  string ident;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("countableType");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  switch (lookahead) {
  
    /* range */
    case Token.LeftBracket :
      lookahead = range();
      (* astnode: (SUBR expr expr (EMPTY)) *)
      break;
      
    /* | enumType */
    case Token.LeftParen :
      lookahead = enumType();
      (* astnode: (ENUM identListNode) *)
      break;
      
    /* | countableTypeIdent range? */
    case Token.Identifier :
      lookahead = qualident();
      /* astnode: (IDENT ident) | (QUALIDENT q0 q1 q2 ... qN ident) */
      id = ast;
      
      /* range? */
      if (lookahead == Token.LeftBracket) {
        lookahead = range();
        ast.ReplaceSubnode(2, id);
      /* astnode: (SUBR expr expr identNode) */
      } /* end if */
      break;
      
    default : /* unreachable code */
      /* fatal error -- abort */
      Environment.Exit(-1);
    } /* end switch */
  
  /* AST node is passed through in ast */
  
  return lookahead;
} /* end countableType */


/* --------------------------------------------------------------------------
 * private method arrayType()
 * --------------------------------------------------------------------------
 * arrayType :=
 *   ARRAY countableType ( ',' countableType )* OF type
 *   ;
 *
 * astnode: (ARRAY indexTypeListNode arrayBaseTypeNode)
 * ----------------------------------------------------------------------- */

private Token arrayType () {
  AstNode idxlist, basetype;
  Fifo tmplist;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("arrayType");
  } /* end if */
  
  /* ARRAY */
  lookahead = lexer.ConsumeSym();
  
  /* countableType */
  if (matchSet(FIRST(COUNTABLE_TYPE), FOLLOW(ARRAY_TYPE))) {
    lookahead = countableType();
    tmplist = Fifo.NewQueue(ast);
    
    /* ( ',' countableType )* */
    while (lookahead == Token.Comma) {
      /* ',' */
      lookahead = lexer.ConsumeSym();
      
      if (matchSet(FIRST(COUNTABLE_TYPE), RESYNC(TYPE_OR_COMMA_OR_OF))) {
        lookahead = countableType();
        tmplist.Enqueue(ast);
      }
      else /* resync */ {
        lookahead = lexer.NextSym();
      } /* end if */
    } /* end while */

    /* OF */
    if (matchToken(Token.OF, FOLLOW(ARRAY_TYPE))) {
      lookahead = lexer.ConsumeSym();
  
      /* type */
      if (matchSet(FIRST(TYPE), FOLLOW(ARRAY_TYPE))) {
        lookahead = type();
        basetype = ast;
      } /* end if */
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  idxlist = AstNode.NewListNode(AST.INDEXLIST, tmplist);
  ast = AstNode.NewNode(AST.ARRAY, idxlist, basetype);
  tmplist.ReleaseQueue();
  
  return lookahead;
} /* end arrayType */


/* --------------------------------------------------------------------------
 * private method extensibleRecordType()
 * --------------------------------------------------------------------------
 * For use with compiler option --no-variant-records.
 *
 * recordType := extensibleRecordType ;
 *
 * extensibleRecordType :=
 *   RECORD ( '(' baseType ')' )? fieldListSequence END
 *   ;
 *
 * baseType := typeIdent ;
 *
 * astnode:
 *  (RECORD fieldListSeqNode) | (EXTREC baseTypeNode fieldListSeqNode)
 * ----------------------------------------------------------------------- */

private Token extensibleRecordType () {
  AstNode basetype, flseq;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("extensibleRecordType");
  } /* end if */
  
  /* RECORD */
  lookahead = lexer.ConsumeSym();
  
  /* ( '(' baseType ')' )? */
  if (lookahead == Token.LeftParen) {
    /* '(' */
    lookahead = lexer.ConsumeSym();
    
    /* baseType */
    if (matchToken(Token.Identifier, FIRST(FIELD_LIST_SEQUENCE))) {
      lookahead = qualident();
      basetype = ast;
      
      /* ')' */
      if (matchToken(Token.RightParen, FIRST(FIELD_LIST_SEQUENCE))) {
        lookahead = lexer.ConsumeSym();
      }
      else /* resync */ {
        lookahead = lexer.NextSym();
      } /* end if */
    }
    else  /* resync */ {
      lookahead = lexer.NextSym();
    } /* end if */
  }
  else {
    basetype = null;
  } /* end if */
  
  /* check for empty field list sequence */
  if (lookahead == Token.END) {

      /* empty field list sequence warning */
      //m2c_emit_warning_w_pos
      //  (M2C_EMPTY_FIELD_LIST_SEQ,
      //   m2c_lexer_lookahead_line(p->lexer),
      //   m2c_lexer_lookahead_column(p->lexer));
      //p->warning_count++;
      
      /* END */
      lookahead = lexer.ConsumeSym();
  }
  
  /* fieldListSequence */
  else if (matchSet(FIRST(FIELD_LIST_SEQUENCE),
           FOLLOW(EXTENSIBLE_RECORD_TYPE))) {
    lookahead = fieldListSequence();
    flseq = ast;
    
    /* END */
    if (matchToken(Token.END, FOLLOW(EXTENSIBLE_RECORD_TYPE))) {
      lookahead = lexer.ConsumeSym();
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in p->ast */
  if (basetype == null) {
    ast = AstNode.NewNode(AST.RECORD, flseq);
  }
  else {
    ast = AstNode.NewNode(AST.EXTREC, basetype, flseq);
  } /* end if */
  
  return lookahead;
} /* end extensibleRecordType */


/* --------------------------------------------------------------------------
 * private method fieldListSequence()
 * --------------------------------------------------------------------------
 * For use with compiler option --no-variant-records.
 *
 * fieldListSequence :=
 *   fieldList ( ';' fieldList )*
 *   ;
 *
 * astnode: (FIELDLISTSEQ fieldListNode+)
 * ----------------------------------------------------------------------- */

private Token fieldListSequence () {
  Fifo tmplist;
  Token lookahead;
  uint lineOfSemicolon, columnOfSemicolon;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("extensibleRecordType");
  } /* end if */
  
  /* fieldList */
  lookahead = fieldList();
  tmplist = Fifo.NewQueue(ast);
  
  /* ( ';' fieldList )* */
  while (lookahead == Token.Semicolon) {
    /* ';' */
    lineOfSemicolon = lexer.LookaheadLine();
    columnOfSemicolon = lexer.LookaheadColumn();
    lookahead = lexer.ConsumeSym();
    
    /* check if semicolon occurred at the end of a field list sequence */
    if (IsElement(FOLLOW(FIELD_LIST_SEQUENCE), lookahead)) {
    
      if (CompilerOptions.ErrantSemicolons()) {
        /* treat as warning */
        //m2c_emit_warning_w_pos
        //  (M2C_SEMICOLON_AFTER_FIELD_LIST_SEQ,
        //   lineOfSemicolon, columnOfSemicolon);
        //warning_count++;
      }
      else /* treat as error */ {
        //m2c_emit_error_w_pos
        //  (M2C_SEMICOLON_AFTER_FIELD_LIST_SEQ,
        //   lineOfSemicolon, columnOfSemicolon);
        //error_count++;
      } /* end if */
      
      /* print source line */
      if (CompilerOptions.Verbose()) {
        lexer.PrintLineAndMarkColumn(lineOfSemicolon, columnOfSemicolon);
      } /* end if */
    
      /* leave field list sequence loop to continue */
      break;
    } /* end if */
    
    /* fieldList */
    if (matchSet(FIRST(VARIABLE_DECLARATION), RESYNC(SEMICOLON_OR_END))) {
      lookahead = fieldList();
      tmplist.Enqueue(ast);
    } /* end if */
  } /* end while */
  
  /* build AST node and pass it back in p->ast */
  ast = AstNode.NewListNode(AST.FIELDLISTSEQ, tmplist);
  tmplist.ReleaseQueue();
  
  return lookahead;
} /* end fieldListSequence */


/* --------------------------------------------------------------------------
 * private method fieldList()
 * --------------------------------------------------------------------------
 * fieldList :=
 *   identList ':' type
 *   ;
 *
 * astnode: (FIELDLIST identListNode typeConstructorNode)
 * ----------------------------------------------------------------------- */

/* TO DO: add discrete first and follow set for fieldList */

private Token fieldList () {
  AstNode idlist, tc;
  Token lookahead;
    
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("fieldList");
  } /* end if */
  
  /* IdentList */
  lookahead = identList();
  idlist = ast;
  
  /* ':' */
  if (matchToken(Token.Colon, FOLLOW(VARIABLE_DECLARATION))) {
    lookahead = lexer.ConsumeSym();
    
    /* type */
    if (matchSet(FIRST(TYPE), FOLLOW(VARIABLE_DECLARATION))) {
      lookahead = type();
      tc = ast;
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.FIELDLIST, idlist, tc);
  
  return lookahead;
} /* end fieldList */


/* --------------------------------------------------------------------------
 * private method variantRecordType()
 * --------------------------------------------------------------------------
 * For use with compiler option --variant-records.
 *
 * recordType := variantRecordType ;
 *
 * variantRecordType :=
 *   RECORD variantFieldListSeq END
 *   ;
 *
 * astnode: (RECORD fieldListSeqNode) | (VRNTREC variantFieldListSeqNode)
 * ----------------------------------------------------------------------- */

private Token variantRecordType () {
  AstNode flseq;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("variantRecordType");
  } /* end if */
  
  /* RECORD */
  lookahead = lexer.ConsumeSym();
  
  /* check for empty field list sequence */
  if (lookahead == Token.END) {

      /* empty field list sequence warning */
      //m2c_emit_warning_w_pos
      //  (M2C_EMPTY_FIELD_LIST_SEQ,
      //   m2c_lexer_lookahead_line(p->lexer),
      //   m2c_lexer_lookahead_column(p->lexer));
      //warning_count++;
      
      /* END */
      lookahead = lexer.ConsumeSym();
  }
  /* variantFieldListSeq */
  else if(matchSet(FIRST(VARIANT_FIELD_LIST_SEQ),
          FOLLOW(VARIANT_RECORD_TYPE))) {
    lookahead = variantFieldListSeq();
    flseq = ast;
    
    /* END */
    if (matchToken(Token.END, FOLLOW(VARIANT_RECORD_TYPE))) {
      lookahead = lexer.ConsumeSym();
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in p->ast */
  if (flseq.NodeType() == AST_VFLISTSEQ) {
    ast = AstNode.NewNode(AST_VRNTREC, flseq);
  }
  else /* not variant field list sequence */ {
    ast = AstNode.NewNode(AST.RECORD, flseq);
  } /* end if */
  
  return lookahead;
} /* end variantRecordType */


/* --------------------------------------------------------------------------
 * private method variantFieldListSeq()
 * --------------------------------------------------------------------------
 * For use with compiler option --variant-records.
 *
 * variantFieldListSeq :=
 *   variantFieldList ( ';' variantFieldList )*
 *   ;
 *
 * astnode:
 *  (FIELDLISTSEQ fieldListNode+) | (VFLISTSEQ anyFieldListNode+)
 * ----------------------------------------------------------------------- */

private Token variantFieldListSeq () {
  Fifo tmplist;
  Token lookahead;
  uint lineOfSemicolon, columnOfSemicolon;
  bool variantFieldListFound = false;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("variantRecordType");
  } /* end if */
  
  /* variantFieldList */
  lookahead = variantFieldList();
  tmplist = Fifo.NewQueue(ast);
  
  if (ast.NodeType() == AST.VFLIST) {
    variantFieldListFound = true;
  } /* end if */
  
  /* ( ';' variantFieldList )* */
  while (lookahead == Token.Semicolon) {
    /* ';' */
    lineOfSemicolon = lexer.LookaheadLine();
    columnOfSemicolon = lexer.LookaheadColumn();
    lookahead = lexer.ConsumeSym();
    
    /* check if semicolon occurred at the end of a field list sequence */
    if (IsElement(FOLLOW(VARIANT_FIELD_LIST_SEQ), lookahead)) {
    
      if (CompilerOptions.ErrantSemicolons()) {
        /* treat as warning */
        //m2c_emit_warning_w_pos
        //  (M2C_SEMICOLON_AFTER_FIELD_LIST_SEQ,
        //   lineOfSemicolon, columnOfSemicolon);
        //warning_count++;
      }
      else /* treat as error */ {
        //m2c_emit_error_w_pos
        //  (M2C_SEMICOLON_AFTER_FIELD_LIST_SEQ,
        //   lineOfSemicolon, columnOfSemicolon);
        //error_count++;
      } /* end if */
      
      /* print source line */
      if (CompilerOptions.Verbose()) {
        lexer.PrintLineAndMarkColumn(lineOfSemicolon, columnOfSemicolon);
      } /* end if */
    
      /* leave field list sequence loop to continue */
      break;
    } /* end if */
    
    /* variantFieldList */
    if (matchSet(FIRST(VARIANT_FIELD_LIST),
        FOLLOW(VARIANT_FIELD_LIST))) {
      lookahead = variantFieldList();
      tmplist.Enqueue(ast);
      
      if (ast.NodeType() == AST.VFLIST) {
        variantFieldListFound = true;
      } /* end if */
    } /* end if */
  } /* end while */
  
  /* build AST node and pass it back in p->ast */
  if (variantFieldListFound) {
    ast = AstNode.NewListNode(AST.VFLISTSEQ, tmplist);
  }
  else /* not variant field list */ {
    ast = AstNode.NewListNode(AST.FIELDLISTSEQ, tmplist);
  } /* end if */
  
  tmplist.ReleaseQueue();
  
  return lookahead;
} /* end variantFieldListSeq */


/* --------------------------------------------------------------------------
 * private method variantFieldList()
 * --------------------------------------------------------------------------
 * For use with compiler option --variant-records.
 *
 * variantFieldList :=
 *   fieldList | variantFields
 *   ;
 * ----------------------------------------------------------------------- */

private Token variantFieldList () {
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("variantFieldList");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  /* fieldList */
  if (lookahead == Token.Identifier) {
    lookahead = fieldList();
  }
  /* | variantFields */
  else if (lookahead == Token.CASE) {
    lookahead = variantFields();
  }
  else /* unreachable code */ {
    /* fatal error -- abort */
      Environment.Exit(-1);
  } /* end if */
  
  /* AST node is passed through in ast */
  
  return lookahead;
} /* end variantFieldList */


/* --------------------------------------------------------------------------
 * private method variantFields()
 * --------------------------------------------------------------------------
 * For use with compiler option --variant-records.
 *
 * variantFields :=
 *   CASE Ident? ':' typeIdent OF
 *   variant ( '|' variant )*
 *   ( ELSE fieldListSequence )?
 *   END
 *   ;
 *
 * astnode:
 *  (VFLIST caseIdentNode caseTypeNode variantListNode fieldListSeqNode)
 * ----------------------------------------------------------------------- */

private Token variantFields () {
  m2c_astnode_t caseid, typeid, vlist, flseq;
  m2c_fifo_t tmplist;
  m2c_string_t ident;
  m2c_token_t lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("variantFields");
  } /* end if */
  
  /* CASE */
  lookahead = lexer.ConsumeSym();
  
  /* Ident? */
  if (lookahead == Token.Identifier) {
    lookahead = lexer.ConsumeSym();
    ident = lexer.CurrentLexeme();
    caseid = AstNode.NewTerminalNode(AST.IDENT, ident);
  }
  else {
    caseid = AstNode.EmptyNode();
  } /* end if */
  
  /* ':' */
  if (matchToken(Token.Colon, RESYNC(ELSE_OR_END))) {
    lookahead = lexer.ConsumeSym();
    
    /* typeIdent */
    if (matchToken(Token.Identifier, RESYNC(ELSE_OR_END))) {
      lookahead = lexer.ConsumeSym();
      ident = lexer.CurrentLexeme();
      typeid = AstNode.NewTerminalNode(AST.IDENT, ident);
    
      /* OF */
      if (matchToken(Token.OF, RESYNC(ELSE_OR_END))) {
        lookahead = lexer.ConsumeSym();
      
        /* variant */
        if (matchSet(FIRST(VARIANT), RESYNC(ELSE_OR_END))) {
          lookahead = variant();
          tmplist = Fifo.NewQueue(ast);
        
          /* ( '|' variant )* */
          while (lookahead == Token.Bar) {
          
            /* '|' */
            lookahead = lexer.ConsumeSym();
          
            /* variant */
            if (matchSet(FIRST(VARIANT), RESYNC(ELSE_OR_END))) {
              lookahead = variant();
              tmplist.Enqueue(ast);
            } /* end if */
          } /* end while */
        } /* end if */
      } /* end if */
    } /* end if */
  } /* end if */
  
  /* resync */
  lookahead = lexer.NextSym();
    
  /* ( ELSE fieldListSequence )? */
  if (lookahead == Token.ELSE) {
  
    /* ELSE */
    lookahead = lexer.ConsumeSym();
  
    /* check for empty field list sequence */
    if (lookahead == Token.END) {

        /* empty field list sequence warning */
        //m2c_emit_warning_w_pos
        //  (M2C_EMPTY_FIELD_LIST_SEQ,
        //   m2c_lexer_lookahead_line(p->lexer),
        //   m2c_lexer_lookahead_column(p->lexer));
        //warning_count++;
    }
    /* fieldListSequence */
    else if (matchSet(
             FIRST(FIELD_LIST_SEQUENCE), FOLLOW(VARIANT_FIELDS))) {
      lookahead = fieldListSequence();
      flseq = ast;
    }
    else /* resync */ {
      lookahead = lexer.NextSym();
    } /* end if */
  }
  else {
    flseq = AstNode.EmptyNode();
  } /* end if */
  
  /* END */
  if (matchToken(Token.END, FOLLOW(VARIANT_FIELDS))) {
    lookahead = lexer.ConsumeSym();
  } /* end if */
  
  /* build AST node and pass it back in ast */
  vlist = AstNode.NewListNode(AST.VARIANTLIST, tmplist);
  ast = AstNode.NewNode(AST.VFLIST, caseid, typeid, vlist, flseq);
  tmplist.ReleaseQueue();
  
  return lookahead;
} /* end variantFields */


/* --------------------------------------------------------------------------
 * private method variant()
 * --------------------------------------------------------------------------
 * For use with compiler option --variant-records.
 *
 * variant :=
 *   caseLabelList ':' variantFieldListSeq
 *   ;
 *
 * astnode: (VARIANT caseLabelListNode fieldListSeqNode)
 * ----------------------------------------------------------------------- */

private Token variant () {
  AstNode cllist, flseq;
  Token lookahead;
    
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("variant");
  } /* end if */
  
  /* caseLabelList */
  lookahead = caseLabelList();
  cllist = ast;
  
  /* ':' */
  if (matchToken(Token.Colon, FOLLOW(VARIANT))) {
    lookahead = lexer.ConsumeSym();
    
    /* check for empty field list sequence */
    if (IsElement(FOLLOW(VARIANT), lookahead)) {

        /* empty field list sequence warning */
        //m2c_emit_warning_w_pos
        //  (M2C_EMPTY_FIELD_LIST_SEQ,
        //   m2c_lexer_lookahead_line(p->lexer),
        //   m2c_lexer_lookahead_column(p->lexer));
        //warning_count++;
    }
    /* variantFieldListSeq */
    else if (matchSet(FIRST(VARIANT_FIELD_LIST_SEQ), FOLLOW(VARIANT))) {
      lookahead = variantFieldListSeq();
      flseq = ast;
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in p->ast */
  ast = AstNode.NewNode(AST.VARIANT, cllist, flseq);
  
  return lookahead;
} /* end variant */


/* --------------------------------------------------------------------------
 * private method caseLabelList()
 * --------------------------------------------------------------------------
 * caseLabelList :=
 *   caseLabels ( ',' caseLabels )*
 *   ;
 *
 * astnode : (CLABELLIST caseLabelsNode+)
 * ----------------------------------------------------------------------- */

private Token caseLabelList () {
  Fifo tmplist;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("caseLabelList");
  } /* end if */
  
  /* caseLabels */
  lookahead = caseLabels();
  tmplist = Fifo.NewQueue(ast);
  
  /* ( ',' caseLabels )* */
  while (lookahead == Token.Comma) {
    /* ',' */
    lookahead = lexer.ConsumeSym();
    
    /* caseLabels */
    if (matchSet(FIRST(CASE_LABELS), FOLLOW(CASE_LABEL_LIST))) {
      lookahead = caseLabels();
      tmplist.Enqueue(ast);
    } /* end if */
  } /* end while */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewListNode(AST.CLABELLIST, tmplist);
  tmplist.ReleaseNode();
  
  return lookahead;
} /* end caseLabelList */


/* --------------------------------------------------------------------------
 * private method caseLabels()
 * --------------------------------------------------------------------------
 * caseLabels :=
 *   constExpression ( '..' constExpression )?
 *   ;
 *
 * astnode: (CLABELS exprNode exprNode)
 * ----------------------------------------------------------------------- */

private Token caseLabels () {
  AstNode lower, upper;
  Token lookahead;
    
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("caseLabels");
  } /* end if */
  
  /* constExpression */
  lookahead = constExpression();
  lower = ast;
  
  /* ( '..' constExpression )? */
  if (lookahead == Token.Range) {
    lookahead = lexer.ConsumeSym();
    
    /* constExpression */
    if (matchSet(FIRST(EXPRESSION), FOLLOW(CASE_LABELS))) {
      lookahead = constExpression();
      upper = ast;
    } /* end if */
  }
  else {
    upper = AstNode.EmptyNode();
  } /* end if */
  
  /* build AST node and pass it back in ast */
  AstNode.NewNode(AST.CLABELS, lower, upper);
  
  return lookahead;
} /* end caseLabels */


/* --------------------------------------------------------------------------
 * private method pointerType()
 * --------------------------------------------------------------------------
 * pointerType :=
 *   POINTER TO type
 *   ;
 *
 * astnode: (POINTER typeConstructorNode)
 * ----------------------------------------------------------------------- */

m2c_token_t pointerType (m2c_parser_context_t p) {
  AstNode tc;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("pointerType");
  } /* end if */
  
  /* POINTER */
  lookahead = lexer.ConsumeSym();
  
  /* TO */
  if (matchToken(Token.TO, FOLLOW(POINTER_TYPE))) {
    lookahead = lexer.ConsumeSym();
    
    /* type */
    if (matchSet(FIRST(TYPE), FOLLOW(POINTER_TYPE))) {
      lookahead = type();
      tc = ast;
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.POINTER, tc);
  
  return lookahead;
} /* end pointerType */


/* --------------------------------------------------------------------------
 * private method procedureType()
 * --------------------------------------------------------------------------
 * procedureType :=
 *   PROCEDURE ( '(' ( formalType ( ',' formalType )* )? ')' )?
 *   ( ':' returnedType )?
 *   ;
 *
 * returnedType := typeIdent ;
 *
 * astnode: (PROCTYPE formalTypeListNode returnedTypeNode)
 * ----------------------------------------------------------------------- */

private Token procedureType () {
  AstNode ftlist, rtype;
  Fifo tmplist;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("procedureType");
  } /* end if */
  
  /* PROCEDURE */
  lookahead = lexer.ConsumeSym();
  
  tmplist = Fifo.NewQueue();
  
  /* ( '(' ( formalType ( ',' formalType )* )? ')' )? */
  if (lookahead == Token.LeftParen) {
    /* '(' */
    lookahead = lexer.ConsumeSym();
    
    /* formalType */
    if (lookahead != Token.RightParen) {
      if (matchSet(FIRST(FORMAL_TYPE), RESYNC(COMMA_OR_RIGHT_PAREN))) {
        lookahead = formalType();
        tmplist.Enqueue(ast);
      }
      else /* resync */ {
        lookahead = lexer.NextSym();
      } /* end if */
      
      /* ( ',' formalType )* */
      while (lookahead == Token.Comma) {
        /* ',' */
        lookahead = lexer.ConsumeSym();
      
        /* formalType */
        if (matchSet(FIRST(FORMAL_TYPE), RESYNC(COMMA_OR_RIGHT_PAREN))) {
          lookahead = formalType();
          tmplist.Enqueue(ast);
        }
        else /* resync */ {
          lookahead = lexer.NextSym();
        } /* end if */
      } /* end while */
    } /* end if */
    
    /* ')' */
    if (matchToken(Token.RightParen, RESYNC(COLON_OR_SEMICOLON))) {
      lookahead = lexer.ConsumeSym();
    }
    else /* resync */ {
      lookahead = lexer.NextSym();
    } /* end if */
  } /* end if */
  
  /* ( ':' returnedType )? */
  if (lookahead == Token.Colon) {
    /* ':' */
    lookahead = lexer.ConsumeSym();
    
    /* returnedType */
    if (matchToken(Token.Identifier, FOLLOW(PROCEDURE_TYPE))) {
      lookahead = qualident();
      rtype = ast;
    }
    else /* resync */ {
      lookahead = lexer.NextSym();
    } /* end if */
  }
  else {
    rtype = AstNode.EmptyNode();
  } /* end if */
  
  /* build formal type list node */
  if (tmplist.EntryCount() > 0) {
    ftlist = AstNode.NewListNode(AST.FTYPELIST, tmplist);
  }
  else /* no formal type list */ {
    ftlist = AstNode.EmptyNode();
  } /* end if */
  
  tmplist.ReleaseQueue();
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.PROCTYPE, ftlist, rtype);
  
  return lookahead;
} /* end procedureType */


/* --------------------------------------------------------------------------
 * private function formalType()
 * --------------------------------------------------------------------------
 * formalType :=
 *   simpleFormalType | attributedFormalType
 *   ;
 * ----------------------------------------------------------------------- */

private Token formalType () {
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("procedureType");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  /* simpleFormalType */
  if (Capabilities.VariadicParameters() && (lookahead == Token.ARGLIST)) {
    lookahead = simpleFormalType();
  }
  else if ((lookahead == Token.ARRAY) || (lookahead == Token.Identifier)) {
    lookahead = simpleFormalType();
  }
  /* | attributedFormalType */
  else if ((lookahead == Token.CONST) || (lookahead == Token.VAR)) {
    lookahead = attributedFormalType();
  }
  else /* unreachable code */ {
    /* fatal error -- abort */
    Environment.Exit(-1);
  } /* end if */
  
  /* AST node is passed through in ast */
  
  return lookahead;
} /* end formalType */


/* --------------------------------------------------------------------------
 * private method simpleFormalType()
 * --------------------------------------------------------------------------
 * simpleFormalType :=
 *   ( ( ARGLIST | ARRAY ) OF )? typeIdent
 *   ;
 *
 * astnode:
 *  (IDENT ident) | (QUALIDENT q0 q1 q2 ... qN ident) | (OPENARRAY identNode)
 * ----------------------------------------------------------------------- */

private Token simpleFormalType () {
  AstNode id;
  Token lookahead;
  bool isArglist = false;
  bool isOpenArray = false;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("simpleFormalType");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  /* ( ARGLIST OF */
  if (Capabilities.VariadicParameters() && (lookahead == Token.ARGLIST)) {
    lookahead = lexer.ConsumeSym();
    isArglist = true;

    /* OF */
    if (matchToken(Token.OF, FOLLOW(SIMPLE_FORMAL_TYPE))) {
      lookahead = lexer.ConsumeSym();
    }
    else /* resync */ {
      lookahead = m2c_next_sym(p->lexer);
    } /* end if */
  }
  /* | ARRAY OF )? */
  else if (lookahead == Token.ARRAY) {
    lookahead = lexer.ConsumeSym();
    isOpenArray = true;

    /* OF */
    if (matchToken(Token.OF, FOLLOW(SIMPLE_FORMAL_TYPE))) {
      lookahead = lexer.ConsumeSym();
    }
    else /* resync */ {
      lookahead = m2c_next_sym(p->lexer);
    } /* end if */
  } /* end if */

  /* typeIdent */
  if (matchToken(p, Token.Identifier, FOLLOW(SIMPLE_FORMAL_TYPE))) {
    lookahead = qualident();
    id = ast;
    /* astnode: (IDENT ident) | (QUALIDENT q0 q1 q2 ... qN ident) */
  } /* end if */

  /* build AST node and pass it back in ast */

  if (isArglist) {
    ast = AstNode.NewNode(AST.ARGLIST, id);
    /* astnode: (ARGLIST identNode) */
  }
  else if (isOpenArray) {
    ast = AstNode.NewNode(AST.OPENARRAY, id);
    /* astnode: (OPENARRAY identNode) */
  } /* end if */
  
  return lookahead;
} /* end simpleFormalType */


/* --------------------------------------------------------------------------
 * private method attributedFormalType()
 * --------------------------------------------------------------------------
 * attributedFormalType :=
 *   ( CONST | VAR ) simpleFormalType
 *   ;
 *
 * astnode: (CONSTP simpleFormalTypeNode) | (VARP simpleFormalTypeNode)
 * ----------------------------------------------------------------------- */

private Token attributedFormalType () {
  AstNode sftype;
  Token lookahead;
  bool isConstAttr = false;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("attributedFormalType");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  /* CONST */
  if (lookahead == Token.BY) {
    lookahead = lexer.ConsumeSym();
    isConstAttr = true;
  }
  /* | VAR */
  else if (lookahead == Token.BY) {
    lookahead = lexer.ConsumeSym();
  }
  else /* unreachable code */ {
    /* fatal error -- abort */
    Environment.Exit(-1);
  } /* end if */
  
  /* simpleFormalType */
  if (matchSet(FIRST(SIMPLE_FORMAL_TYPE),
      FOLLOW(ATTRIBUTED_FORMAL_TYPE))) {
    lookahead = simpleFormalType();
    sftype = ast;
  } /* end if */
  
  /* build AST node and pass it back in ast */
  if (isConstAttr) {
    ast = AstNode.NewNode(AST.CONSTP, sftype);
  }
  else {
    ast = AstNode.NewNode(AST.VARP, sftype);
  } /* end if */
  
  return lookahead;
} /* end attributedFormalType */


/* --------------------------------------------------------------------------
 * private method procedureHeader()
 * --------------------------------------------------------------------------
 * procedureHeader :=
 *   PROCEDURE procedureSignature
 *   ;
 * ----------------------------------------------------------------------- */

private Token procedureHeader () {
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("procedureHeader");
  } /* end if */
  
  /* PROCEDURE */
  lookahead = lexer.ConsumeSym();
  
  /* procedureSignature */
  if (matchToken(Token.Identifier, FOLLOW(PROCEDURE_HEADER))) {
    lookahead = procedureSignature();
  } /* end if */
  
  /* AST node is passed through in ast */
  
  return lookahead;
} /* end procedureHeader */


/* --------------------------------------------------------------------------
 * private method procedureSignature()
 * --------------------------------------------------------------------------
 * procedureSignature :=
 *   Ident ( '(' formalParamList? ')' ( ':' returnedType )? )?
 *   ;
 *
 * astnode: (PROCDEF identNode formalParamListNode returnTypeNode)
 * ----------------------------------------------------------------------- */

private Token procedureSignature () {
  AstNode id, fplist, rtype;
  string ident;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("procedureSignature");
  } /* end if */
  
  /* Ident */
  lookahead = lexer.ConsumeSym();
  ident = lexer.CurrentLexeme();
  id = AstNode.NewTerminalNode(AST.IDENT, ident);
  
  /* ( '(' formalParamList? ')' ( ':' returnedType )? )? */
  if (lookahead == Token.LeftParen) {
    
    /* '(' */
    lookahead = lexer.ConsumeSym();
    
    /* formalParamList? */
    if ((lookahead == Token.Identifier) ||
        (lookahead == Token.VAR)) {
      lookahead = formalParamList();
      fplist = ast;
    }
    else {
      fplist = AstNode.EmptyNode();
    } /* end if */
    
    /* ')' */
    if (matchToken(Token.RightParen, FOLLOW(PROCEDURE_TYPE))) {
      lookahead = lexer.ConsumeSym();
    }
    else /* resync */ {
      lookahead = lexer.NextSym();
    } /* end if */
    
    /* ( ':' returnedType )? */
    if (lookahead == Token.Colon) {
      /* ':' */
      lookahead = lexer.ConsumeSym();
    
      /* returnedType */
      if (matchToken(Token.Identifier, FOLLOW(PROCEDURE_TYPE))) {
        lookahead = qualident();
        rtype = ast;
      } /* end if */
    }
    else {
      rtype = AstNode.EmptyNode();
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.PROCDEF, id, fplist, rtype);
  
  return lookahead;
} /* end procedureSignature */


/* --------------------------------------------------------------------------
 * private method formalParamList()
 * --------------------------------------------------------------------------
 * formalParamList :=
 *   formalParams ( ';' formalParams )*
 *   ;
 *
 * astnode: (FPARAMLIST fparams+)
 * ----------------------------------------------------------------------- */

private Token formalParamList () {
  Fifo tmplist;
  Token lookahead;
  uint lineOfSemicolon, columnOfSemicolon;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("formalParamList");
  } /* end if */
  
  /* formalParams */
  lookahead = formalParams();
  tmplist = Fifo.NewQueue(ast);
  
  /* ( ';' formalParams )* */
  while (lookahead == Token.Semicolon) {
    /* ';' */
    lineOfSemicolon = lexer.LookaheadLine();
    columnOfSemicolon = lexer.LookaheadColumn();
    lookahead = lexer.ConsumeSym();
    
    /* check if semicolon occurred at the end of a formal parameter list */
    if (lookahead == Token.RightParen) {
    
      if (CompilerOptions.ErrantSemicolons()) {
        /* treat as warning */
        //m2c_emit_warning_w_pos
        //  (M2C_SEMICOLON_AFTER_FORMAL_PARAM_LIST,
        //   lineOfSemicolon, columnOfSemicolon);
        warningCount++;
      }
      else /* treat as error */ {
        //m2c_emit_error_w_pos
        //  (M2C_SEMICOLON_AFTER_FORMAL_PARAM_LIST,
        //   lineOfSemicolon, columnOfSemicolon);
        errorCount++;
      } /* end if */
      
      /* print source line */
      if (CompilerOptions.Verbose()) {
        //m2c_print_line_and_mark_column(p->lexer,
        //  lineOfSemicolon, columnOfSemicolon);
      } /* end if */
    
      /* leave field list sequence loop to continue */
      break;
    } /* end if */
    
    /* formalParams */
    if (matchSet(FIRST(FORMAL_PARAMS), FOLLOW(FORMAL_PARAMS))) {
      lookahead = formalParams();
      Fifo.Enqueue(tmplist, ast);
    } /* end if */
  } /* end while */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewListNode(AST.FPARAMLIST, tmplist);
  Fifo.Release(tmplist);
  
  return lookahead;
} /* end formalParamList */


/* --------------------------------------------------------------------------
 * private method formalParams()
 * --------------------------------------------------------------------------
 * formalParams :=
 *   simpleFormalParams | attribFormalParams
 *   ;
 * ----------------------------------------------------------------------- */

private Token formalParams () {
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("formalParams");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  /* simpleFormalParams */
  if (lookahead == Token.Identifier) {
    lookahead = simpleFormalParams();
  }
  /* | attribFormalParams */
  else if ((lookahead == Token.CONST) || (lookahead == Token.VAR)) {
    lookahead = attribFormalParams();
  }
  else /* unreachable code */ {
    /* fatal error -- abort */
      Environment.Exit(-1);
  } /* end if */
  
  /* AST node is passed through in ast */
  
  return lookahead;
} /* end formalParams */


/* --------------------------------------------------------------------------
 * private method simpleFormalParams()
 * --------------------------------------------------------------------------
 * simpleFormalParams :=
 *   identList ':' simpleFormalType
 *   ;
 *
 * astnode: (FPARAMS identListNode simpleFormalTypeNode)
 * ----------------------------------------------------------------------- */

private Token simpleFormalParams () {
  AstNode idlist, sftype;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("simpleFormalParams");
  } /* end if */
  
  /* IdentList */
  lookahead = identList();
  idlist = ast;
  
  /* ':' */
  if (matchToken(Token.Colon, FOLLOW(SIMPLE_FORMAL_PARAMS))) {
    lookahead = lexer.ConsumeSym();
    
    /* formalType */
    if (matchSet(FIRST(FORMAL_TYPE), FOLLOW(SIMPLE_FORMAL_PARAMS))) {
      lookahead = simpleFormalType();
      sftype = ast;
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in p->ast */
  ast = AstNode.NewNode(AST.FPARAMS, idlist, sftype, NULL);
  
  return lookahead;
} /* end simpleFormalParams */


/* --------------------------------------------------------------------------
 * private method attribFormalParams()
 * --------------------------------------------------------------------------
 * attribFormalParams :=
 *   ( CONST | VAR ) simpleFormalParams
 *   ;
 *
 * astnode: (FPARAMS identListNode formalTypeNode)
 * ----------------------------------------------------------------------- */

private Token attribFormalParams () {
  AstNode aftype, sftype;
  Token lookahead;
  bool isConstAttr = false;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("attribFormalParams");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  /* CONST */
  if (lookahead == Token.CONST) {
    lookahead = lexer.ConsumeSym();
    isConstAttr = true;
  }
  /* | VAR */
  else if (lookahead == Token.VAR) {
    lookahead = lexer.ConsumeSym();
  }
  else /* unreachable code */ {
    /* fatal error -- abort */
    Environment.Exit(-1);
  } /* end if */
  
  /* simpleFormalParams */
  if (matchSet(FIRST(SIMPLE_FORMAL_PARAMS),
      FOLLOW(ATTRIB_FORMAL_PARAMS))) {
    lookahead = simpleFormalParams();
  } /* end if */
  
  /* build AST node and pass it back in p->ast */
  sftype = AstNode.SubnodeForIndex(ast, 1);
  
  if (isConstAttr) {
    aftype = AstNode.NewNode(AST.CONSTP, sftype);
  }
  else {
    aftype = AstNode.NewNode(AST.VARP, sftype);
  } /* end if */
  
  AstNode.ReplaceSubnode(ast, 1, aftype);
  
  return lookahead;
} /* end attribFormalParams */


/* ************************************************************************ *
 * Implementation and Program Module Syntax                                 *
 * ************************************************************************ */


/* --------------------------------------------------------------------------
 * private method implementationModule()
 * --------------------------------------------------------------------------
 * implementationModule :=
 *   IMPLEMENTATION programModule
 *   ;
 * ----------------------------------------------------------------------- */

private Token implementationModule () {
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("implementationModule");
  } /* end if */
  
  /* IMPLEMENTATION */
  lookahead = lexer.ConsumeSym();
  
  /* programModule */
  if (matchToken(Token.MODULE, FOLLOW(PROGRAM_MODULE))) {
    lookahead = programModule();
  } /* end if */
  
  /* AST node is passed through in ast */
  
  return lookahead;
} /* end implementationModule */


/* --------------------------------------------------------------------------
 * private method programModule()
 * --------------------------------------------------------------------------
 * programModule :=
 *   MODULE moduleIdent modulePriority? ';'
 *   import* block moduleIdent '.'
 *   ;
 *
 * moduleIdent := Ident ;
 *
 * astnode: (IMPMOD identNode priorityNode importListNode blockNode)
 * ----------------------------------------------------------------------- */

private Token programModule () {
  AstNode id, prio, implist, body;
  string ident1, ident2;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("programModule");
  } /* end if */
  
  /* MODULE */
  lookahead = lexer.ConsumeSym();
  
  /* moduleIdent */
  if (matchToken(Token.Identifier, RESYNC(IMPORT_OR_BLOCK))) {
    lookahead = lexer.ConsumeSym();
    ident1 = lexer.CurrentLexeme();
    
    /* modulePriority? */
    if (lookahead == Token.LeftBracket) {
      lookahead = modulePriority();
      prio = ast;
    }
    else /* no module priority */ {
      prio = AstNode.EmptyNode();
    } /* end while */
    
    /* ';' */
    if (matchToken(Token.Semicolon, RESYNC(IMPORT_OR_BLOCK))) {
      lookahead = lexer.ConsumeSym();
    }
    else /* resync */ {
      lookahead = lexer.NextSym();
    } /* end if */
  }
  else /* resync */ {
    lookahead = lexer.NextSym();
  } /* end if */
  
  tmplist = Fifo.NewQueue();
  
  /* import* */
  while ((lookahead == Token.IMPORT) ||
         (lookahead == Token.FROM)) {
    lookahead = import();
    Fifo.Enqueue(tmplist, ast);
  } /* end while */
  
  if (Fifo.EntryCount(tmplist) > 0) {
    implist = AstNode.NewListNode(AST.IMPLIST, tmplist);
  }
  else /* no import list */ {
    implist = AstNode.EmptyNode();
  } /* end if */
  
  Fifo.ReleaseQueue(tmplist);
  
  /* block */
  if (matchSet(FIRST(BLOCK), FOLLOW(PROGRAM_MODULE))) {
    lookahead = block();
    body = ast;
    
    /* moduleIdent */
    if (matchToken(Token.Identifier, FOLLOW(PROGRAM_MODULE))) {
      lookahead = lexer.ConsumeSym();
      ident2 = lexer.CurrentLexeme();
      
      if (ident1 != ident2) {
        /* TO DO: report error -- module identifiers don't match */ 
      } /* end if */
      
      if (matchToken(Token.Period, FOLLOW(PROGRAM_MODULE))) {
        lookahead = lexer.ConsumeSym();
      } /* end if */
    } /* end if */
  } /* end if */  
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.IMPMOD, id, prio, implist, body);
  
  return lookahead;
} /* end programModule */


/* --------------------------------------------------------------------------
 * private method modulePriority()
 * --------------------------------------------------------------------------
 * modulePriority :=
 *   '[' constExpression ']'
 *   ;
 * ----------------------------------------------------------------------- */

private Token modulePriority (m2c_parser_context_t p) {
  m2c_token_t lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("modulePriority");
  } /* end if */
  
  /* '[' */
  lookahead = lexer.ConsumeSym();
  
  /* constExpression */
  if (matchSet(FIRST(EXPRESSION), FOLLOW(MODULE_PRIORITY))) {
    lookahead = constExpression();
    
    /* ']' */
    if (matchToken(Token.RightBracket, FOLLOW(MODULE_PRIORITY))) {
      lookahead = lexer.ConsumeSym();
    } /* end if */
  } /* end if */
  
  /* AST node is passed through in ast */
  
  return lookahead;
} /* end modulePriority */


/* --------------------------------------------------------------------------
 * private method block()
 * --------------------------------------------------------------------------
 * block :=
 *   declaration* ( BEGIN statementSequence )? END
 *   ;
 *
 * astnode: (BLOCK declarationListNode statementSeqNode)
 * ----------------------------------------------------------------------- */

private Token block () {
  AstNode decllist, stmtseq;
  Fifo tmplist;
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("block");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  tmplist = Fifo.NewQueue();
  
  /* declaration* */
  while ((lookahead == Token.CONST) ||
         (lookahead == Token.TYPE) ||
         (lookahead == Token.VAR) ||
         (lookahead == Token.PROCEDURE) ||
         (lookahead == Token.MODULE)) {
    lookahead = declaration();
    Fifo.Enqueue(tmplist, ast);
  } /* end while */
  
  if (Fifo.EntryCount(tmplist) > 0) {
    decllist = AstNode.NewListNode(AST.DECLLIST, tmplist);
  }
  else /* no declarations */ {
    decllist = AstNode.EmptyNode();
  } /* end if */
  
  Fifo.Release(tmplist);
  
  /* ( BEGIN statementSequence )? */
  if (lookahead == TOKEN.BEGIN) {
    lookahead = lexer.ConsumeSym();
    
    /* check for empty statement sequence */
    if ((isElement(FOLLOW(STATEMENT_SEQUENCE), lookahead))) {
    
        /* print warning */
        //m2c_emit_warning_w_pos
        //  (M2C_EMPTY_STMT_SEQ,
        //   lexer.LookaheadLine(),
        //   lexer.LookaheadColumn());
        //warning_count++;
    }
    /* statementSequence */
    else if (matchSet(FIRST(STATEMENT_SEQUENCE), FOLLOW(STATEMENT))) {
      lookahead = statementSequence();
      stmtseq = ast;
    }
    else /* resync */ {
      lookahead = lexer.NextSym();
    } /* end if */
  }
  else /* no statement sequence */ {
    stmtseq = AstNode.EmptyNode();
  } /* end if */
  
  /* END */
  if (matchToken(Token.END, FOLLOW(BLOCK))) {
    lookahead = lexer.ConsumeSym();
  } /* end if */
  
  /* build AST node and pass it back in p->ast */
  ast = AstNode.NewNode(AST.BLOCK, decllist_node, stmtseq);
  
  return lookahead;
} /* end block */


/* --------------------------------------------------------------------------
 * private function declaration()
 * --------------------------------------------------------------------------
 * declaration :=
 *   CONST ( constDeclaration ';' )* |
 *   TYPE ( typeDeclaration ';' )* |
 *   VAR ( variableDeclaration ';' )* |
 *   procedureDeclaration ';'
 *   moduleDeclaration ';'
 *   ;
 * 
 * constDeclaration := constDefinition ;
 * ----------------------------------------------------------------------- */

private Token declaration () {
  Token lookahead;
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("declaration");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  switch (lookahead) {
    
    /* CONST */
    case Token.CONST :
      lookahead = lexer.ConsumeSym();
      
      /* ( constDeclaration ';' )* */
      while (lookahead == Token.Identifier) {
        lookahead = constDefinition();
        
        /* ';' */
        if (matchToken(Token.Semicolon,
            RESYNC(DECLARATION_OR_IDENT_OR_SEMICOLON))) {
          lookahead = lexer.ConsumeSym();
        } /* end if */
      } /* end while */
      break;
      
    /* | TYPE */
    case Token.TYPE :
      lookahead = lexer.ConsumeSym();
      
      /* ( typeDeclaration ';' )* */
      while (lookahead == Token.Identifier) {
        lookahead = typeDeclaration();
        
        /* ';' */
        if (matchToken(Token.Semicolon,
            RESYNC(DECLARATION_OR_IDENT_OR_SEMICOLON))) {
          lookahead = lexer.ConsumeSym();
        } /* end if */
      } /* end while */
      break;
      
    /* | VAR */
    case Token.VAR :
      lookahead = lexer.ConsumeSym();
      
      /* ( variableDeclaration ';' )* */
      while (lookahead == Token.Identifier) {
        lookahead = variableDeclaration();
        
        /* ';' */
        if (matchToken(Token.Semicolon,
            RESYNC(DECLARATION_OR_IDENT_OR_SEMICOLON))) {
          lookahead = lexer.ConsumeSym();
        } /* end if */
      } /* end while */
      break;
      
    /* | procedureDeclaration ';' */
    case Token.PROCEDURE :
      lookahead = procedureDeclaration();
      
      /* ';' */
      if (matchToken(Token.Semicolon,
          RESYNC(DECLARATION_OR_SEMICOLON))) {
        lookahead = lexer.ConsumeSym();
      } /* end if */
      break;
      
    /* | moduleDeclaration ';' */
    case Token.MODULE :
      lookahead = moduleDeclaration();
      
      /* ';' */
      if (matchToken(Token.Semicolon,
          RESYNC(DECLARATION_OR_SEMICOLON))) {
        lookahead = lexer.ConsumeSym();
      } /* end if */
      break;
      
    default : /* unreachable code */
      /* fatal error -- abort */
      Environment.Exit(-1);
  } /* end switch */
  
  /* AST node is passed through in ast */
  
  return lookahead;
} /* end declaration */


/* --------------------------------------------------------------------------
 * private method typeDeclaration()
 * --------------------------------------------------------------------------
 * typeDeclaration :=
 *   Ident '=' ( type | varSizeRecordType )
 *   ;
 *
 * astnode: (TYPEDECL identNode typeConstructorNode)
 * ----------------------------------------------------------------------- */

private Token typeDeclaration () {
  AstNode id, tc;
  string ident;
  Token lookahead;
    
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("typeDeclaration");
  } /* end if */
  
  /* Ident */
  lookahead = lexer.ConsumeSym();
  ident = lexer.CurrentLexeme();
  id = AstNode.NewTerminalNode(AST.IDENT, ident);
  
  /* '=' */
  if (matchToken(Token.Equal, FOLLOW(TYPE_DECLARATION))) {
    lookahead = lexer.ConsumeSym();
    
    /* type | varSizeRecordType */
    if (matchSet(FIRST(TYPE_DECLARATION_TAIL),
        FOLLOW(TYPE_DECLARATION))) {
      
      /* type */
      if (lookahead != Token.VAR) {
        lookahead = type();
        tc = ast;
      }
      /* | varSizeRecordType */
      else {
        lookahead = varSizeRecordType();
        tc = ast;
      } /* end if */
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.TYPEDECL, id, tc);
  
  return lookahead;
} /* end typeDeclaration */


/* --------------------------------------------------------------------------
 * private method varSizeRecordType()
 * --------------------------------------------------------------------------
 * varSizeRecordType :=
 *   VAR RECORD fieldListSequence
 *   VAR varSizeFieldIdent ':' ARRAY sizeFieldIdent OF typeIdent
 *   END
 *   ;
 *
 * astnode:
 *  (VSREC fieldListSeqNode (VSFIELD identNode identNode identNode))
 * ----------------------------------------------------------------------- */

private Token varSizeRecordType () {
  AstNode flseq, vsfield, vsfieldid, sizeid, typeid;
  string ident;
  uint lineOfSemicolon, columnOfSemicolon;
  Token lookahead;
    
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("varSizeRecordType");
  } /* end if */
  
  /* VAR */
  lookahead = lexer.ConsumeSym();
  
  /* RECORD */
  if (matchToken(Token.RECORD, FOLLOW(VAR_SIZE_RECORD_TYPE))) {
    lookahead = lexer.ConsumeSym();
    
    /* check for empty field list sequence */
    if (lookahead == Token.VAR) {

        /* empty field list sequence warning */
        //m2c_emit_warning_w_pos
        //  (M2C_EMPTY_FIELD_LIST_SEQ,
        //   lexer.LookaheadLine(),
        //   lexer.LookaheadColumn());
        //warningCount++;
    }
    /* fieldListSequence */
    else if (matchSet(FIRST(FIELD_LIST_SEQUENCE),
             FOLLOW(VAR_SIZE_RECORD_TYPE))) {
      lookahead = fieldListSequence();
      flseq = ast;
    }
    else /* resync */ {
      lookahead = lexer.NextSym();
    } /* end if */
    
    /* VAR */
    if (matchToken(Token.VAR, FOLLOW(VAR_SIZE_RECORD_TYPE))) {
      lookahead = lexer.ConsumeSym();
      
      if (lookahead == Token.END) {
        //m2c_emit_warning_w_pos
        //  (M2C_EMPTY_FIELD_LIST_SEQ,
        //   lexer.LookaheadLine(),
        //   lexer.LookaheadColumn());
        //   lexer.ConsumeSym();
        //warning_count++;
      }
      /* varSizeFieldIdent */
      else if (matchToken(Token.Identifier,
               FOLLOW(VAR_SIZE_RECORD_TYPE))) {
        lookahead = lexer.ConsumeSym();
        ident = lexer.CurrentLexeme();
        vsfieldid = AstNode.NewTerminalNode(AST.IDENT, ident);
      
        /* ':' */
        if (matchToken(Token.Colon, FOLLOW(VAR_SIZE_RECORD_TYPE))) {
          lookahead = lexer.ConsumeSym();
        
          /* ARRAY */
          if (match_token(Token.ARRAY, FOLLOW(VAR_SIZE_RECORD_TYPE))) {
            lookahead = lexer.ConsumeSym();
          
            /* sizeFieldIdent */
            if (matchToken(Token.Identifier,
                FOLLOW(VAR_SIZE_RECORD_TYPE))) {
              lookahead = lexer.ConsumeSym();
              ident = lexer.CurrentLexeme();
              sizeid = AstNode.NewTerminalNode(AST.IDENT, ident);
            
              /* OF */
              if (matchToken(Token.OF, FOLLOW(VAR_SIZE_RECORD_TYPE))) {
                lookahead = lexer.ConsumeSym();
              
                /* typeIdent */
                if (matchToken(Token.Identifier,
                    FOLLOW(VAR_SIZE_RECORD_TYPE))) {
                  lookahead = qualident();
                  typeid = ast;
                  
                  /* check for errant semicolon */
                  if (lookahead == Token.Semicolon) {
                    lineOfSemicolon = lexer.LookaheadLine();
                    columnOfSemicolon = lexer.LookaheadColumn();
                  
                    if (CompilerOptions.ErrantSemicolons()) {
                      /* treat as warning */
                      //m2c_emit_warning_w_pos
                      //  (M2C_SEMICOLON_AFTER_FIELD_LIST_SEQ,
                      //   lineOfSemicolon, columnOfSemicolon);
                      //warningCount++;
                    }
                    else /* treat as error */ {
                      //m2c_emit_error_w_pos
                      //  (M2C_SEMICOLON_AFTER_FIELD_LIST_SEQ,
                      //   lineOfSemicolon, columnOfSemicolon);
                      //errorCount++;
                    } /* end if */
                    
                    lexer.ConsumeSym();
                    
                    /* print source line */
                    if (CompilerOptions.Verbose()) {
                      lexer.PrintLineAndMarkColumn
                        (lineOfSemicolon, columnOfSemicolon);
                    } /* end if */
                  } /* end if */
                  
                  if (matchToken(Token.END,
                      FOLLOW(VAR_SIZE_RECORD_TYPE))) {
                    lookahead = lexer.ConsumeSym();
                  } /* end if */
                } /* end if */
              } /* end if */
            } /* end if */
          } /* end if */
        } /* end if */
      } /* end if */
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  vsfield = AstNode.NewNode(AST.VSFIELD, vsfieldid, sizeid, typeid);
  ast = AstNode.NewNode(AST.VSREC, flseq, vsfield);
  
  return lookahead;
} /* end varSizeRecordType */


/* --------------------------------------------------------------------------
 * private method variableDeclaration()
 * --------------------------------------------------------------------------
 * variableDeclaration :=
 *   identList ':' type
 *   ;
 *
 * astnode: (VARDECL identListNode typeConstructorNode)
 * ----------------------------------------------------------------------- */

private Token variableDeclaration () {
  AstNode idlist, tc;
  Token lookahead;

  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("variableDeclaration");
  } /* end if */
  
  /* IdentList */
  lookahead = identList();
  idlist = ast;
  
  /* ':' */
  if (matchToken(Token.Colon, FOLLOW(VariableDeclaration))) {
    lookahead = lexer.ConsumeSym();
    
    /* type */
    if (matchSet(FIRST(Type), FOLLOW(VariableDeclaration))) {
      lookahead = type();
      tc = ast;
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.VARDECL, idlist, tc);
  
  return lookahead;
} /* variableDeclaration */


/* --------------------------------------------------------------------------
 * private method procedureDeclaration()
 * --------------------------------------------------------------------------
 * procedureDeclaration :=
 *   procedureHeader ';' block Ident
 *   ;
 *
 * astnode: (PROC procDefinitionNode blockNode)
 * ----------------------------------------------------------------------- */

private Token procedureDeclaration () {
  AstNode procdef, body;
  string ident;
  Token lookahead;

  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("procedureDeclaration");
  } /* end if */
  
  /* procedureHeader */
  lookahead = procedureHeader();
  procdef = ast;
  
  /* ';' */
  if (matchToken(Token.Semicolon, FOLLOW(ProcedureDeclaration))) {
    lookahead = lexer.ConsumeSym();
    
    /* block */
    if (matchSet(FIRST(Block), FOLLOW(ProcedureDeclaration))) {
      lookahead = block();
      body = ast;
      
      /* Ident */
      if (matchToken(Token.Identifier, FOLLOW(ProcedureDeclaration))) {
        lookahead = lexer.ConsumeSym();
        ident = lexer.CurrentLexeme();
        
        /* TO DO: check if procedure identifiers match */
      } /* end if */
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.PROC, procdef, body);
  
  return lookahead;
} /* procedureDeclaration */


/* --------------------------------------------------------------------------
 * private method moduleDeclaration()
 * --------------------------------------------------------------------------
 * moduleDeclaration :=
 *   MODULE moduleIdent modulePriority? ';'
 *   import* export? block moduleIdent
 *   ;
 *
 * astnode:
 *  (MODDECL identNode prioNode importListNode exportListNode blockNode)
 * ----------------------------------------------------------------------- */

private Token moduleDeclaration () {
  AstNode id, prio, implist, exp, body;
  Fifo tmplist;
  string ident1, ident2;
  Token lookahead;

  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("moduleDeclaration");
  } /* end if */

  if (Capabilities.LocalModules() == false) {
    // MODULE declaration not supported -- report error and skip
  } /* end if */
  
  /* MODULE */
  lookahead = lexer.ConsumeSym();
  
  /* moduleIdent */
  if (matchToken(Token.Identifier, RESYNC(IMPORT_OR_BLOCK))) {
    lookahead = lexer.ConsumeSym();
    ident1 = lexer.CurrentLexeme();
    
    /* modulePriority? */
    if (lookahead == Token.LeftBracket) {
      lookahead = modulePriority();
      prio = ast;
    }
    else /* no module priority */ {
      prio = AstNode.EmptyNode();
    } /* end while */
    
    /* ';' */
    if (matchToken(Token.Semicolon, RESYNC(IMPORT_OR_BLOCK))) {
      lookahead = lexer.ConsumeSym();
    }
    else /* resync */ {
      lookahead = lexer.NextSym();
    } /* end if */
  }
  else /* resync */ {
    lookahead = lexer.NextSym();
  } /* end if */
  
  tmplist = Fifo.NewQueue();
  
  /* import* */
  while ((lookahead == Token.Import) ||
         (lookahead == Token.From)) {
    lookahead = import();
    tmplist.Enqueue(ast);
  } /* end while */
  
  if (tmplist.EntryCount() > 0) {
    implist = AstNode.NewListNode(AST.IMPLIST, tmplist);
  }
  else /* no import list */ {
    implist = AstNode.EmptyNode();
  } /* end if */
  
  tmplist.Release();
  
  /* export? */
  if (lookahead == Token.Export) {
    lookahead = export();
    exp = ast;
  }
  else /* no export list */ {
    exp = AstNode.EmptyNode();
  } /* end while */
  
  /* block */
  if (matchSet(FIRST(Block), FOLLOW(ModuleDeclaration))) {
    lookahead = block();
    body = ast;
    
    /* moduleIdent */
    if (matchToken(Token.Identifier, FOLLOW(ModuleDeclaration))) {
      lookahead = lexer.ConsumeSym();
      ident2 = lexer.CurrentLexeme();
      
      if (ident1 != ident2) {
        /* TO DO: report error -- module identifiers don't match */ 
      } /* end if */
    } /* end if */
  } /* end if */  
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.MODDECL, id, prio, implist, exp, body);
  
  return lookahead;
} /* moduleDeclaration */


/* --------------------------------------------------------------------------
 * private method export()
 * --------------------------------------------------------------------------
 * export :=
 *   EXPORT QUALIFIED? identList ';'
 *   ;
 *
 * astnode: (EXPORT identListNode) | (QUALEXP identListNode)
 * ----------------------------------------------------------------------- */

private Token export () {
  AstNode idlist;
  bool qualified;
  Token lookahead;

  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("export");
  } /* end if */
    
  /* EXPORT */
  lookahead = lexer.ConsumeSym();
    
  /* QUALIFIED? */
  if (lookahead == Token.Qualified) {
    lookahead = ConsumeSym();
    qualified = true;
  }
  else {
    qualified = false;
  } /* end if */
  
  /* identList */
  if (matchToken(Token.Identifier, FOLLOW(Export))) {
    lookahead = identList();
    idlist = ast;
    
    /* ';' */
    if (matchToken(Token.Semicolon, FOLLOW(Export))) {
      lookahead = lexer.ConsumeSym();
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in p->ast */
  if (qualified) {
    ast = AstNode.NewNode(AST.QUALEXP, idlist);
  }
  else /* unqualified */ {
    ast = AstNode.NewNode(AST.EXPORT, idlist);
  } /* end if */
  
  return lookahead;
} /* export */


/* --------------------------------------------------------------------------
 * private method statementSequence()
 * --------------------------------------------------------------------------
 * statementSequence :=
 *   statement ( ';' statement )*
 *   ;
 *
 * astnode: (STMTSEQ stmtNode+)
 * ----------------------------------------------------------------------- */

private Token statementSequence () {
  Fifo tmplist; 
  Token lookahead;

  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("statementSequence");
  } /* end if */
  
  /* statement */
  lookahead = statement();
  tmplist = Fifo.NewQueue(ast);
  
  /* ( ';' statement )* */
  while (lookahead == Token.Semicolon) {
    /* ';' */
    lineOfSemicolon = lexer.LookaheadLine();
    columnOfSemicolon = lexer.LookaheadColumn();
    lookahead = lexer.ConsumeSym();
    
    /* check if semicolon occurred at the end of a statement sequence */
    if (FOLLOW(StatementSequence).IsElement(lookahead)) {
    
      if (CompilerOptions.ErrantSemicolon()) {
        /* treat as warning */
        // m2c_emit_warning_w_pos
        //   (M2C_SEMICOLON_AFTER_STMT_SEQ,
        //    lineOfSemicolon, columnOfSemicolon);
        // warningCount++;
      }
      else /* treat as error */ {
        // m2c_emit_error_w_pos
        //   (M2C_SEMICOLON_AFTER_STMT_SEQ,
        //    lineOfSemicolon, columnOfSemicolon);
        // errorCount++;
      } /* end if */
      
      /* print source line */
      if (CompilerOptions.Verbose()) {
        lexer.PrintLineAndMarkColumn(lineOfSemicolon, columnOfSemicolon);
      } /* end if */
      
      /* leave statement sequence loop to continue */
      break;
    } /* end if */
    
    /* statement */
    if (matchSet(FIRST(Statement),
        RESYNC(FIRST_OR_FOLLOW_OF_STATEMENT))) {
      lookahead = statement();
      tmplist.Enqueue(ast);
    } /* end if */
  } /* end while */
  
  /* build AST node and pass it back in p->ast */
  ast = AstNode.NewListNode(AST.STMTSEQ, tmplist);
  tmplist.Release();
  
  return lookahead;
} /* statementSequence */


/* --------------------------------------------------------------------------
 * private method statement()
 * --------------------------------------------------------------------------
 * statement :=
 *   assignmentOrProcCall | returnStatement | withStatement | ifStatement |
 *   caseStatement | loopStatement | whileStatement | repeatStatement |
 *   forStatement | EXIT
 *   ;
 * ----------------------------------------------------------------------- */

private Token statement () {
  Token lookahead;

  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("statement");
  } /* end if */
  
  lookahead = lexer.NextSym();
  
  switch (lookahead) {
  
    /* assignmentOrProcCall */
    case Token.Identifier :
      lookahead = assignmentOrProcCall();
      break;
      
    /* | returnStatement */
    case Token.Return :
      lookahead = returnStatement();
      break;
      
    /* | withStatement */
    case Token.WITH :
      lookahead = withStatement();
      break;
      
    /* | ifStatement */
    case Token.IF :
      lookahead = ifStatement();
      break;
      
    /* | caseStatement */
    case Token.CASE :
      lookahead = caseStatement();
      break;
      
    /* | loopStatement */
    case Token.LOOP :
      lookahead = loopStatement();
      break;
      
    /* | whileStatement */
    case Token.WHILE :
      lookahead = whileStatement();
      break;
      
    /* | repeatStatement */
    case Token.REPEAT :
      lookahead = repeatStatement();
      break;
      
    /* | forStatement */
    case Token.FOR :
      lookahead = forStatement();
      break;
      
    /* | EXIT */
    case Token.EXIT :
      lookahead = ConsumeSym();
      ast = AstNode.NewNode(AST.EXIT);
      break;
      
    default : /* unreachable code */
      /* fatal error -- abort */
      Environment.Exit(-1);
    } /* end switch */
  
  return lookahead;
} /* statement */


/* --------------------------------------------------------------------------
 * private method assignmentOrProcCall()
 * --------------------------------------------------------------------------
 * assignmentOrProcCall :=
 *   designator ( ':=' expression | actualParameters )?
 *   ;
 *
 * astnode:
 *  (ASSIGN designatorNode exprNode) | (PCALL designatorNode argsNode)
 * ----------------------------------------------------------------------- */

private Token assignmentOrProcCall () {
  AstNode desig;
  Token lookahead;

  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("assignmentOrProcCall");
  } /* end if */
  
  /* designator */
  lookahead = designator();
  desig = ast;
  
  /* ( ':=' expression | actualParameters )? */
  if (lookahead == TOKEN_ASSIGN) {
    lookahead = lexer.ConsumeSym();
    
    /* expression */
    if (matchSet(FIRST(expression), FOLLOW(ASSIGNMENT_OR_PROC_CALL))) {
      lookahead = expression();
      ast = AstNode.NewNode(AST.ASSIGN, desig, ast);
      /* astnode: (ASSIGN designatorNode exprNode) */
    } /* end if */
  }
  /* | actualParameters */
  else if (lookahead == Token.LeftParen) {
    lookahead = actualParameters();
    ast = AstNode.NewNode(AST.PCALL, desig, ast);
    /* astnode: (PCALL designatorNode argsNode) */
  } /* end if */
  
  return lookahead;
} /* assignmentOrProcCall */


/* --------------------------------------------------------------------------
 * private method actualParameters()
 * --------------------------------------------------------------------------
 * actualParameters :=
 *   '(' ( expression ( ',' expression )* )? ')'
 *   ;
 *
 * astnode: (ARGS exprNode+) | (EMPTY)
 * ----------------------------------------------------------------------- */

private Token actualParameters () {
  Fifo tmplist;
  Token lookahead;

  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("actualParameters");
  } /* end if */
  
  /* '(' */
  lookahead = lexer.ConsumeSym();
  
  /* ( expression ( ',' expression )* )? */
  if (FIRST(expression).IsMember(lookahead)) {
    /* expression */
    lookahead = expression();
    tmplist = Fifo.NewQueue(ast);
  
    /* ( ',' expression )* */
    while (lookahead == Token.Comma) {
      /* ',' */
      lookahead = lexer.ConsumeSym();
    
      /* expression */
      if (matchSet(FIRST(expression), FOLLOW(expression))) {
        lookahead = expression();
        tmplist.Enqueue(ast);
      } /* end if */
    } /* end while */
    
    ast = AstNode.NewListNode(AST.ARGS, tmplist);
    tmplist.Release();
  }
  else /* no arguments */ {
    ast = AstNode.EmptyNode();
  } /* end if */
  
  /* ')' */
  if (matchToken(Token.RightParen, FOLLOW(actualParameters))) {
    lookahead = lexer.ConsumeSym();
  } /* end if */
  
  return lookahead;
} /* actualParameters */


/* --------------------------------------------------------------------------
 * private method returnStatement()
 * --------------------------------------------------------------------------
 * returnStatement :=
 *   RETURN expression?
 *   ;
 *
 * astnode: (RETURN exprNode) | (RETURN (EMPTY))
 * ----------------------------------------------------------------------- */

private Token returnStatement () {
  AstNode expr;
  Token lookahead;

  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("returnStatement");
  } /* end if */
  
  /* RETURN */
  lookahead = lexer.ConsumeSym();
  
  /* expression? */
  if (FIRST(expression).IsElement(lookahead)) {
    lookahead = expression();
    expr = ast;
  }
  else {
    expr = AstNode.EmptyNode();
  } /* end if */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.RETURN, expr);
  
  return lookahead;
} /* returnStatement */


/* --------------------------------------------------------------------------
 * private method withStatement()
 * --------------------------------------------------------------------------
 * withStatement :=
 *   WITH designator DO statementSequence END
 *   ;
 *
 * astnode: (WITH designatorNode statementSeqNode)
 * ----------------------------------------------------------------------- */

private Token withStatement () {
  AstNode desig, stmtseq;
  Token lookahead;

  if (Capabilities.WithStatement() == false) {
    // WITH statement not supported -- report error and skip
  } /* end if */
  
  if (CompilerOptions.ParserDebug()) {
    PARSER_DEBUG_INFO("withStatement");
  } /* end if */
  
  /* WITH */
  lookahead = lexer.ConsumeSym();
  
  /* designator */
  if (matchToken(Token.Identifier, FOLLOW(withStatement))) {
    lookahead = designator();
    desig = ast;
    
    /* DO */
    if (matchToken(Token.DO, FOLLOW(withStatement))) {
      lookahead = lexer.ConsumeSym();
      
      /* check for empty statement sequence */
      if (lookahead == Token.END) {
    
        /* empty statement sequence warning */
        // m2c_emit_warning_w_pos
        //   (M2C_EMPTY_STMT_SEQ,
        //    lexer.LookaheadLine(),
        //    lexer.LookaheadColumn());
        // warning_count++;
             
        /* END */
        lookahead = lexer.ConsumeSym();
      }
      /* statementSequence */
      else if (matchSet(FIRST(statementSequence), FOLLOW(withStatement))) {
        lookahead = statementSequence();
        stmtseq = ast;
        
        /* END */
        if (matchToken(Token.END, FOLLOW(withStatement))) {
          lookahead = lexer.ConsumeSym();
        } /* end if */
      } /* end if */
    } /* end if */
  } /* end if */
  
  /* build AST node and pass it back in ast */
  ast = AstNode.NewNode(AST.WITH, desig, stmtseq);
  
  return lookahead;
} /* withStatement */


/* --------------------------------------------------------------------------
 * private method ifStatement()
 * --------------------------------------------------------------------------
 * ifStatement :=
 *   IF boolExpression THEN statementSequence
 *   ( ELSIF boolExpression THEN statementSequence )*
 *   ( ELSE statementSequence )?
 *   END
 *   ;
 *
 * boolExpression := expression ;
 *
 * astnode: (IF exprNode ifBranchNode elsifSeqNode elseBranchNode)
 * ----------------------------------------------------------------------- */

private Token ifStatement () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* ifStatement */


/* --------------------------------------------------------------------------
 * private method caseStatement()
 * --------------------------------------------------------------------------
 * caseStatement :=
 *   CASE expression OF case ( '|' case )*
 *   ( ELSE statementSequence )?
 *   END
 *   ;
 *
 * NB: 'case' is a reserved word in C#, we use caseBranch() here instead
 *
 * astnode: (SWITCH exprNode (CASELIST caseBranchNode+) elseBranchNode)
 * ----------------------------------------------------------------------- */

private Token caseStatement () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* caseStatement */


/* --------------------------------------------------------------------------
 * private method caseBranch()
 * --------------------------------------------------------------------------
 * case :=
 *   caseLabelList ':' statementSequence
 *   ;
 *
 * NB: 'case' is a reserved word in C#, we use caseBranch() here instead
 *
 * astnode: (CASE caseLabelListNode statementSeqNode)
 * ----------------------------------------------------------------------- */

private Token caseBranch () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* caseBranch */


/* --------------------------------------------------------------------------
 * private method loopStatement()
 * --------------------------------------------------------------------------
 * loopStatement :=
 *   LOOP statementSequence END
 *   ;
 *
 * astnode: (LOOP statementSeqNode)
 * ----------------------------------------------------------------------- */

private Token loopStatement () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* loopStatement */


/* --------------------------------------------------------------------------
 * private method whileStatement()
 * --------------------------------------------------------------------------
 * whileStatement :=
 *   WHILE boolExpression DO statementSequence END
 *   ;
 *
 * boolExpression := expression ;
 *
 * astnode: (WHILE exprNode statementSeqNode)
 * ----------------------------------------------------------------------- */

private Token whileStatement () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* whileStatement */


/* --------------------------------------------------------------------------
 * private method repeatStatement()
 * --------------------------------------------------------------------------
 * repeatStatement :=
 *   REPEAT statementSequence UNTIL boolExpression
 *   ;
 *
 * boolExpression := expression ;
 *
 * astnode: (REPEAT statementSeqNode exprNode)
 * ----------------------------------------------------------------------- */

private Token repeatStatement () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* repeatStatement */


/* --------------------------------------------------------------------------
 * private method forStatement()
 * --------------------------------------------------------------------------
 * forStatement :=
 *   FOR forLoopVariant ':=' startValue TO endValue
 *   ( BY stepValue )? DO statementSequence END
 *   ;
 *
 * forLoopVariant := Ident ;
 *
 * startValue, endValue := ordinalExpression ;
 *
 * ordinalExpression := expression
 *
 * stepValue := constExpression
 *
 * astnode: (FORTO identNode exprNode exprNode exprNode statementSeqNode)
 * ----------------------------------------------------------------------- */

private Token forStatement () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* forStatement */


/* --------------------------------------------------------------------------
 * private method designator()
 * --------------------------------------------------------------------------
 * designator :=
 *   qualident ( '^' | selector )*
 *   ;
 *
 * astnode: identNode | (DEREF expr) | (DESIG headNode tailNode)
 * ----------------------------------------------------------------------- */

private Token designator () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* designator */


/* --------------------------------------------------------------------------
 * private method selector()
 * --------------------------------------------------------------------------
 * selector :=
 *   '.' Ident | '[' indexList ']'
 *   ;
 *
 * astnode: (FIELD identNode) | (INDEX exprNode+)
 * ----------------------------------------------------------------------- */

private Token selector () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* selector */


/* --------------------------------------------------------------------------
 * private method indexList()
 * --------------------------------------------------------------------------
 * indexList :=
 *   expression ( ',' expression )*
 *   ;
 *
 * astnode: (INDEX exprNode+)
 * ----------------------------------------------------------------------- */

private Token indexList () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* indexList */


/* --------------------------------------------------------------------------
 * private method expression()
 * --------------------------------------------------------------------------
 * expression :=
 *   simpleExpression ( operL1 simpleExpression )?
 *   ;
 *
 * operL1 := '=' | '#' | '<' | '<=' | '>' | '>=' | IN ;
 *
 * astnode:
 *  (EQ expr expr) | (NEQ expr expr) | (LT expr expr) | (LTEQ expr expr) |
 *  (GT expr expr) | (GTEQ expr expr) | (IN expr expr) | simpleExprNode
 * ----------------------------------------------------------------------- */

private Token expression () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* expression */


/* --------------------------------------------------------------------------
 * private method simpleExpression()
 * --------------------------------------------------------------------------
 * simpleExpression :=
 *   ( '+' | '-' )? term ( operL2 term )*
 *   ;
 *
 * operL2 := '+' | '-' | OR ;
 *
 * astnode:
 *  (NEG expr) |
 *  (PLUS expr expr) | (MINUS expr expr) | (OR expr expr) | termNode
 * ----------------------------------------------------------------------- */

private Token simpleExpression () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* simpleExpression */


/* --------------------------------------------------------------------------
 * private method term()
 * --------------------------------------------------------------------------
 * term :=
 *   simpleTerm ( operL3 simpleTerm )*
 *   ;
 *
 * operL3 := '*' | '/' | DIV | MOD | AND ;
 *
 * astnode:
 *  (ASTERISK expr expr) | (SOLIDUS expr expr) |
 *  (DIV expr expr) | (MOD expr expr) | (AND expr expr) | simpleTermNode
 * ----------------------------------------------------------------------- */

private Token term () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* term */


/* --------------------------------------------------------------------------
 * private method simpleTerm()
 * --------------------------------------------------------------------------
 * simpleTerm :=
 *   NOT? factor
 *   ;
 *
 * astnode: (NOT expr) | factorNode
 * ----------------------------------------------------------------------- */

private Token simpleTerm () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* simpleTerm */


/* --------------------------------------------------------------------------
 * private method factor()
 * --------------------------------------------------------------------------
 * factor :=
 *   NumberLiteral | StringLiteral | setValue |
 *   designatorOrFuncCall | '(' expression ')'
 *   ;
 *
 * astnode:
 *  (INTVAL value) | (REALVAL value) | (CHRVAL value) | (QUOTEDVAL value) |
 *  setValNode | designatorNode | funcCallNode | exprNode
 * ----------------------------------------------------------------------- */

private Token factor () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* factor */


/* --------------------------------------------------------------------------
 * private method designatorOrFuncCall()
 * --------------------------------------------------------------------------
 * designatorOrFuncCall :=
 *   designator ( setValue | actualParameters )?
 *   ;
 *
 * astnode:
 *  (SETVAL designatorNode elemListNode) | (FCALL designatorNode argsNode)
 * ----------------------------------------------------------------------- */

private Token designatorOrFuncCall () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* designatorOrFuncCall */


/* --------------------------------------------------------------------------
 * private method setValue()
 * --------------------------------------------------------------------------
 * setValue :=
 *   '{' element ( ',' element )* '}'
 *   ;
 *
 * astnode: (SETVAL (EMPTY) elemListNode)
 * ----------------------------------------------------------------------- */

private Token setValue () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* setValue */


/* --------------------------------------------------------------------------
 * private method element()
 * --------------------------------------------------------------------------
 * element :=
 *   expression ( '..' expression )?
 *   ;
 *
 * astnode: exprNode | (RANGE expr expr)
 * ----------------------------------------------------------------------- */

private Token element () {
  Token lookahead;

  // TO DO
  
  return lookahead;
} /* element */


} /* Parser */

} /* namespace */

/* END OF FILE */