import { cn } from '@/lib/utils'

interface CardProps {
  className?: string
  children: React.ReactNode
}

function Card({ className, children }: CardProps) {
  return (
    <div className={cn('bg-white rounded-xl shadow-sm border border-gray-100', className)}>
      {children}
    </div>
  )
}

interface CardHeaderProps {
  className?: string
  children: React.ReactNode
}

function CardHeader({ className, children }: CardHeaderProps) {
  return (
    <div className={cn('px-6 py-4 border-b border-gray-100', className)}>
      {children}
    </div>
  )
}

interface CardTitleProps {
  className?: string
  children: React.ReactNode
}

function CardTitle({ className, children }: CardTitleProps) {
  return (
    <h3 className={cn('text-lg font-semibold text-gray-900', className)}>
      {children}
    </h3>
  )
}

interface CardDescriptionProps {
  className?: string
  children: React.ReactNode
}

function CardDescription({ className, children }: CardDescriptionProps) {
  return (
    <p className={cn('text-sm text-gray-500 mt-1', className)}>
      {children}
    </p>
  )
}

interface CardContentProps {
  className?: string
  children: React.ReactNode
}

function CardContent({ className, children }: CardContentProps) {
  return (
    <div className={cn('px-6 py-4', className)}>
      {children}
    </div>
  )
}

interface CardFooterProps {
  className?: string
  children: React.ReactNode
}

function CardFooter({ className, children }: CardFooterProps) {
  return (
    <div className={cn('px-6 py-4 border-t border-gray-100 bg-gray-50 rounded-b-xl', className)}>
      {children}
    </div>
  )
}

export { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter }
