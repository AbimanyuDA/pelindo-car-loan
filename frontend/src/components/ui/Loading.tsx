import { Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'

interface SpinnerProps {
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

function Spinner({ size = 'md', className }: SpinnerProps) {
  const sizes = {
    sm: 'w-4 h-4',
    md: 'w-6 h-6',
    lg: 'w-10 h-10'
  }

  return (
    <Loader2 className={cn('animate-spin text-navy-600', sizes[size], className)} />
  )
}

interface LoadingProps {
  message?: string
  className?: string
}

function Loading({ message = 'Memuat data...', className }: LoadingProps) {
  return (
    <div className={cn('flex flex-col items-center justify-center py-12', className)}>
      <Spinner size="lg" />
      <p className="mt-4 text-gray-500">{message}</p>
    </div>
  )
}

interface PageLoadingProps {
  message?: string
}

function PageLoading({ message = 'Memuat halaman...' }: PageLoadingProps) {
  return (
    <div className="min-h-[60vh] flex items-center justify-center">
      <Loading message={message} />
    </div>
  )
}

export { Spinner, Loading, PageLoading }
