# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
Database operation result class for consistent return values across all tools.
"""

from typing import Any, Optional


class DbOperationResult:
    """
    Represents the result of a database operation, including success status, 
    error message, number of rows affected, and any returned data.
    """
    
    def __init__(
        self, 
        success: bool, 
        error: Optional[str] = None, 
        rows_affected: Optional[int] = None, 
        data: Optional[Any] = None
    ):
        """
        Initialize a database operation result.
        
        Args:
            success: Whether the database operation was successful
            error: Error message if the operation failed; otherwise None
            rows_affected: Number of rows affected by the operation, if applicable
            data: Any data returned by the operation, such as query results
        """
        self.success = success
        self.error = error
        self.rows_affected = rows_affected
        self.data = data
    
    def to_dict(self) -> dict:
        """
        Convert the result to a dictionary for JSON serialization.
        
        Returns:
            Dictionary representation of the result
        """
        result = {
            "success": self.success
        }
        
        if self.error is not None:
            result["error"] = self.error
            
        if self.rows_affected is not None:
            result["rows_affected"] = self.rows_affected
            
        if self.data is not None:
            result["data"] = self.data
            
        return result
    
    def __repr__(self) -> str:
        """String representation of the result."""
        return (
            f"DbOperationResult(success={self.success}, "
            f"error={self.error}, rows_affected={self.rows_affected}, "
            f"data={self.data})"
        )
