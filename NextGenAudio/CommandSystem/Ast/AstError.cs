// NextGenAudio - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  NextGenAudio contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System.Text;

namespace NextGenAudio.CommandSystem.Ast
{
	internal class AstError : AstNode
	{
		public override AstType Type => AstType.Error;

		public string Description { get; }

		public AstError(AstNode referenceNode, string description)
			: this(referenceNode.FullRequest, referenceNode.Position, referenceNode.Length, description) { }

		public AstError(string request, int pos, int len, string description) : base(request)
		{
			Position = pos;
			Length = len;
			Description = description;
		}

		public override void Write(StringBuilder strb, int depth)
		{
			strb.AppendLine(FullRequest);
			if (Position == 1) strb.Append('.');
			else if (Position > 1) strb.Append(' ', Position);
			strb.Append('~', Length).Append('^').AppendLine();
			strb.Append("Error: ").AppendLine(Description);
		}
	}
}
